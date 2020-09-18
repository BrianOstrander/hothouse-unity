using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Ai;
using Lunra.Hothouse.Models;
using Lunra.Satchel;
using Lunra.StyxMvp.Models;
using Lunra.StyxMvp.Services;
using UnityEngine;

namespace Lunra.Hothouse.Services
{
	public class LogisticsService : BindableService<GameModel>
	{
		class Context
		{
			public struct NavigationCache
			{
				public string PrimaryId;
				public string SecondaryId;
				public bool IsNavigable;

				public bool IsMatch(string id0, string id1)
				{
					if (PrimaryId == id0) return SecondaryId == id1;
					if (PrimaryId == id1) return SecondaryId == id0;
					return false;
				}
			}
			
			public abstract class Info
			{
				protected Context Context { get; }

				public Info(
					Context context
				)
				{
					Context = context;
				}

				public abstract Container GetContainer();

				public abstract IInventoryModel GetParent();

				public bool ValidNavigationTo(Info info) => ValidNavigationTo(info, out _);

				public bool ValidNavigationTo(
					Info info,
					out NavigationCache cache
				)
				{
					var primaryId = GetParent().Id.Value;
					var secondaryId = info.GetParent().Id.Value;
					
					try
					{
						cache = Context.NavigationCaches.First(c => c.IsMatch(primaryId, secondaryId));
						return cache.IsNavigable;
					}
					catch (InvalidOperationException)
					{
						cache = new NavigationCache();
					}

					cache.PrimaryId = primaryId;
					cache.SecondaryId = secondaryId;
						
					var target = Context.service.Model.Query.FirstOrDefault<IModel>(secondaryId);
						
					if (target != null)
					{
						if (Navigation.TryQuery(target, out var query))
						{
							// TODO: It feels like I should probably cache this navigation check's result...
							cache.IsNavigable = NavigationUtility.CalculateNearest(
								GetParent().Transform.Position.Value,
								out _,
								query
							);
						}
						else Debug.LogError($"Unable to get a navigation query for {target}");
					}
					else Debug.LogError($"Unable to find a model with id {secondaryId}");

					Context.NavigationCaches.Add(cache);

					return cache.IsNavigable;
				}
			}
			
			public class DwellerInfo : Info
			{
				public DwellerModel Dweller { get; }

				public DwellerInfo(
					Context context,
					DwellerModel dweller
				) : base (
					context
				)
				{
					Dweller = dweller;
				}

				public override Container GetContainer() => Dweller.Inventory.Container;
				public override IInventoryModel GetParent() => Dweller;
			}
			
			public abstract class ItemInfo : Info
			{
				public Item Item { get; }

				bool? inventoryFound;
				bool? parentFound;

				public ItemInfo(
					Context context,
					Item item
				) : base (
					context
				)
				{
					Item = item;
				}

				public int GetPriority() => 0;
				
				public override Container GetContainer()
				{
					if (inventoryFound.HasValue)
					{
						if (inventoryFound.Value) return Context.Inventories[Item.ContainerId];
						return null;
					}

					inventoryFound = Context.Inventories.TryGetValue(Item.ContainerId, out var inventory);

					if (inventoryFound.Value) return inventory;

					var parent = GetParent(); 
					
					if (parent == null) return null;

					inventoryFound = true;
					Context.Inventories.Add(Item.ContainerId, parent.Inventory.Container);
					return parent.Inventory.Container;
				}

				public override IInventoryModel GetParent()
				{
					if (parentFound.HasValue)
					{
						if (parentFound.Value) return Context.Parents[Item.ContainerId];
						return null;
					}

					parentFound = Context.Parents.TryGetValue(Item.ContainerId, out var parent);

					if (parentFound.Value) return parent;

					parent = Context.service.Model.Query
						.FirstOrDefault<IInventoryModel>(i => i.Inventory.Container.Id == Item.ContainerId);

					parentFound = parent != null;
					if (parentFound.Value) Context.Parents.Add(Item.ContainerId, parent);
					
					return parent;
				}
			}
			
			public class ResourceInfo : ItemInfo
			{
				bool resourceTypeChecked;
				string resourceType;
				
				public ResourceInfo(
					Context context,
					Item item
				) : base(context, item) { }

				protected string GetResourceType()
				{
					if (resourceTypeChecked) return resourceType;
					resourceTypeChecked = true;
					return (resourceType = Item[Items.Keys.Resource.Type]);
				}
			}
			
			public class ReservationInfo : ItemInfo
			{
				CapacityInfo capacity;
				
				public ReservationInfo(
					Context context,
					Item item
				) : base(context, item) { }

				public CapacityInfo GetCapacity() => capacity ?? (capacity = Context.Capacities[Item[Items.Keys.Reservation.CapacityId]]);
			}
			
			public class CapacityInfo : ItemInfo
			{
				public enum Goals
				{
					Unknown = 0,
					None = 10,
					Receive = 20,
					Distribute = 30
				}
				
				public Goals Goal { get; private set; }
				
				PropertyFilter filter;
				
				public CapacityInfo(
					Context context,
					Item item
				) : base(context, item) { }

				public Goals Calculate()
				{
					var desire = Item[Items.Keys.Capacity.Desire];
					if (desire != Items.Values.Capacity.Desires.NotCalculated)
					{
						if (desire == Items.Values.Capacity.Desires.None) return Goal = Goals.None;
						if (desire == Items.Values.Capacity.Desires.Receive) return Goal = Goals.Receive;
						if (desire == Items.Values.Capacity.Desires.Distribute) return Goal = Goals.Distribute;
						Debug.LogError($"Unrecognized desire: {desire}");
						return Goal = Goals.Unknown;
					}

					var parent = GetParent();

					if (!parent.Inventory.Capacities.TryGetValue(Item.Id, out var filter))
					{
						Debug.LogError($"Cannot find filter [ {Item.Id} ] for capacity {Item} in {parent.ShortId}");
						return Goal = Goals.Unknown;
					}

					var capacityCountTarget = Item[Items.Keys.Capacity.CountTarget];
				
					var inventory = GetContainer();

					var resourceTotalCount = 0;

					foreach (var stack in inventory.Stacks)
					{
						// If this doesn't pass, it means it's not a resource, so we can ignore it...
						if (!Context.Resources.TryGetValue(stack.Id, out var resource)) continue;
						// TODO: Is this actually what I want???
						if (resource.Item[Items.Keys.Resource.LogisticState] != Items.Values.Resource.LogisticStates.None) continue;
						if (!filter.Validate(resource.Item)) continue;
						
						resourceTotalCount += stack.Count;
					}

					var delta = capacityCountTarget - resourceTotalCount;
				
					if (delta == 0)
					{
						// We are satisfied
						Item.Set(
							(Items.Keys.Capacity.Desire, Items.Values.Capacity.Desires.None),
							(Items.Keys.Capacity.CountCurrent, resourceTotalCount)
						);

						return Goal = Goals.None;
					}
					
					if (0 < delta)
					{
						// We want more
						Item.Set(
							(Items.Keys.Capacity.Desire, Items.Values.Capacity.Desires.Receive),
							(Items.Keys.Capacity.CountCurrent, resourceTotalCount)
						);

						inventory.Deposit(
							Context.service.Model.Items.Builder
								.BeginItem()
								.WithProperties(
									Items.Instantiate.Reservation.OfInput(
										Item.Id,
										Item[Items.Keys.Capacity.Pool]
									)
								)
								.Done(delta)
						);
						
						return Goal = Goals.Receive;
					}
					
					// We want less
					Item.Set(
						(Items.Keys.Capacity.Desire, Items.Values.Capacity.Desires.Distribute),
						(Items.Keys.Capacity.CountCurrent, resourceTotalCount)
					);
				
					inventory.Deposit(
						Context.service.Model.Items.Builder
							.BeginItem()
							.WithProperties(
								Items.Instantiate.Reservation.OfOutput(
									Item.Id,
									Item[Items.Keys.Capacity.Pool]
								)
							)
							.Done(Mathf.Abs(delta))
					);
					
					return Goal = Goals.Distribute;
				}

				public PropertyFilter GetFilter() => filter ?? (filter = GetParent().Inventory.Capacities[Item.Id]);
			}
			
			public class CapacityPoolInfo : ItemInfo
			{
				[Flags]
				public enum Operations
				{
					None = 0,
					ForbidPoolReceiving = 1 << 0,
					ApplyModifications = 1 << 1
				}
				
				public CapacityPoolInfo(
					Context context,
					Item item
				) : base(context, item) { }

				public Operations Calculate(
					out CapacityInfo.Goals modificationGoal,
					out CapacityInfo[] modifications
				)
				{
					var isForbidden = Item[Items.Keys.CapacityPool.IsForbidden];
					var countMaximum = Item[Items.Keys.CapacityPool.CountMaximum];
					var countPrevious = Item[Items.Keys.CapacityPool.CountCurrent];
					var countTarget = Item[Items.Keys.CapacityPool.CountTarget];

					CapacityInfo.Goals? possibleGoalUpdate = null;

					if (isForbidden) possibleGoalUpdate = CapacityInfo.Goals.None;
					else if (countTarget < countMaximum) possibleGoalUpdate = CapacityInfo.Goals.Distribute;
					else if (countMaximum < countTarget) possibleGoalUpdate = CapacityInfo.Goals.Receive;
					
					var modificationsList = new List<CapacityInfo>();
					
					var countCurrent = 0;

					foreach (var element in GetContainer().All(i => i.TryGet(Items.Keys.Capacity.Pool, out var poolId) && poolId == Item.Id))
					{
						if (element.Item.NoInstances) continue;
						
						var capacityCountCurrent = element.Item[Items.Keys.Capacity.CountCurrent];
						
						countCurrent += capacityCountCurrent;

						if (!possibleGoalUpdate.HasValue) continue;
						
						if (!Context.Capacities.TryGetValue(element.Item.Id, out var capacityInfo))
						{
							Debug.LogError($"Unable to find cached capacity [ {element.Item.Id} ], are you sure capacities have been calculated already?");
							continue;
						}

						if (possibleGoalUpdate.Value != capacityInfo.Goal) modificationsList.Add(capacityInfo);
					}

					if (countPrevious != countCurrent) Item[Items.Keys.CapacityPool.CountCurrent] = countCurrent;

					modificationGoal = CapacityInfo.Goals.Unknown;

					if (possibleGoalUpdate.HasValue)
					{
						switch (possibleGoalUpdate.Value)
						{
							case CapacityInfo.Goals.None:
								modificationGoal = CapacityInfo.Goals.None;
								break;
							case CapacityInfo.Goals.Receive:
								if (countCurrent < countTarget) modificationGoal = CapacityInfo.Goals.Receive;
								break;
							case CapacityInfo.Goals.Distribute:
								if (countTarget < countCurrent) modificationGoal = CapacityInfo.Goals.Distribute;
								break;
							default:
								Debug.LogError($"Unrecognized goal {possibleGoalUpdate.Value}");
								modificationsList.Clear();
								break;
						}
					}

					modifications = modificationsList.ToArray();

					var result = Operations.None;

					if (countTarget <= countCurrent) result |= Operations.ForbidPoolReceiving;
					if (modifications.Any()) result |= Operations.ApplyModifications;

					return result;
				}
			}

			LogisticsService service;

			public List<NavigationCache> NavigationCaches = new List<NavigationCache>();
			public List<DwellerInfo> Dwellers = new List<DwellerInfo>();
			public Dictionary<long, ResourceInfo> Resources = new Dictionary<long, ResourceInfo>();
			public Dictionary<long, ReservationInfo> Reservations = new Dictionary<long, ReservationInfo>();
			public Dictionary<long, CapacityPoolInfo> CapacityPools = new Dictionary<long, CapacityPoolInfo>();
			public Dictionary<long, CapacityInfo> Capacities = new Dictionary<long, CapacityInfo>();
			public Dictionary<long, Container> Inventories = new Dictionary<long, Container>();
			public Dictionary<long, IInventoryModel> Parents = new Dictionary<long, IInventoryModel>();

			public Context(LogisticsService service) => this.service = service;
			
			public void Clear()
			{
				NavigationCaches.Clear();
				Dwellers.Clear();
				Resources.Clear();
				Reservations.Clear();
				CapacityPools.Clear();
				Capacities.Clear();
				Inventories.Clear();
				Parents.Clear();
			}
		}

		Context context;

		public LogisticsService(GameModel model) : base(model)
		{
			context = new Context(this);
		}
		
		protected override void Bind()
		{
			Model.SimulationUpdate += OnGameSimulationUpdate;
		}

		protected override void UnBind()
		{
			Model.SimulationUpdate -= OnGameSimulationUpdate;
		}
		
		#region GameModel Events
		void OnGameSimulationUpdate()
		{
			// foreach (var inventory in Model.Query.All<IInventoryModel>())
			// {
			// 	inventory.Inventory.Calculate();
			// }
			
			Debug.Break();
			
			return;

			foreach (var dweller in Model.Dwellers.AllActive)
			{
				context.Dwellers.Add(
					new Context.DwellerInfo(
						context,
						dweller
					)	
				);
			}
			
			foreach (var item in Model.Items.All()) OnItemUpdate(item);

			var capacitySources = new Dictionary<long, Context.CapacityInfo>();
			var capacityDestinations = new Dictionary<long, Context.CapacityInfo>();
			
			foreach (var capacity in context.Capacities.Values)
			{
				switch (capacity.Calculate())
				{
					case Context.CapacityInfo.Goals.None:
						break;
					case Context.CapacityInfo.Goals.Distribute:
						capacitySources.Add(capacity.Item.Id, capacity);
						break;
					case Context.CapacityInfo.Goals.Receive:
						capacityDestinations.Add(capacity.Item.Id, capacity);
						break;
					default:
						Debug.LogError($"Unrecognized goal: {capacity.Goal}");
						break;
				}
			}
			
			var capacityPoolForbiddenDestinations = new HashSet<long>();

			foreach (var capacityPool in context.CapacityPools.Values)
			{
				var capacityPoolResult = capacityPool.Calculate(out var modificationGoal, out var modifications);

				if (capacityPoolResult.HasFlag(Context.CapacityPoolInfo.Operations.ForbidPoolReceiving)) capacityPoolForbiddenDestinations.Add(capacityPool.Item.Id);

				if (capacityPoolResult.HasFlag(Context.CapacityPoolInfo.Operations.ApplyModifications))
				{
					// I don't love the way this works, but basically if the pool defines behaviour that conflicts with
					// the capacities below it, we fix those issues here...
					
					foreach (var modification in modifications)
					{
						// Debug.Log($"Moving [ {modification.Capacity.Item.Id} ] from {modification.Begin} to {modification.End}");
						
						switch (modification.Goal)
						{
							case Context.CapacityInfo.Goals.None:
								// No change required if previously had no goal... 
								break;
							case Context.CapacityInfo.Goals.Receive:
								// Changing a destination to something else...
								capacityDestinations.Remove(modification.Item.Id);
								break;
							case Context.CapacityInfo.Goals.Distribute:
								// Changing a source to something else...
								capacitySources.Remove(modification.Item.Id);
								break;
							default:
								Debug.LogError($"Unrecognized goal origin {modification.Goal}");
								break;
						}
						
						switch (modificationGoal)
						{
							case Context.CapacityInfo.Goals.None:
								// We're forbidding stuff, so we don't add it as a source or destination...
								break;
							case Context.CapacityInfo.Goals.Receive:
								// This is now a destination
								capacityDestinations.Add(modification.Item.Id, modification);
								break;
							case Context.CapacityInfo.Goals.Distribute:
								// This is now a source
								capacitySources.Add(modification.Item.Id, modification);
								break;
							default:
								Debug.LogError($"Unrecognized goal origin {modificationGoal}");
								break;
						}
					}	
				}
			}

			var reservationSources = new Dictionary<long, Context.ReservationInfo>();

			foreach (var reservation in context.Reservations.Values)
			{
				if (reservation.Item[Items.Keys.Reservation.LogisticState] == Items.Values.Reservation.LogisticStates.Output)
				{
					reservationSources.Add(reservation.Item.Id, reservation);
				}
			}
			
			// Order in a way that caches will get filled up or taken from last
			var capacityDestinationsSorted = capacityDestinations
				.Values
				.OrderBy(c => c.GetPriority())
				.ToList();

			var dwellers = context.Dwellers
				.Where(m => m.Dweller.InventoryPromises.All.None())
				.ToList();

			// dwellers.Clear();

			// While loop below is entirely for handling assignment of transfers to dwellers.
			while (capacitySources.Any() && capacityDestinations.Any() && dwellers.Any())
			{
				var capacityDestinationCurrent = capacityDestinationsSorted[0];
				capacityDestinationsSorted.RemoveAt(0);
				capacityDestinations.Remove(capacityDestinationCurrent.Item.Id);

				if (capacityPoolForbiddenDestinations.Contains(capacityDestinationCurrent.Item[Items.Keys.Capacity.Pool])) continue;

				var capacityDestinationFilterId = capacityDestinationCurrent.Item.Id;

				if (!capacityDestinationCurrent.GetParent().Inventory.Capacities.TryGetValue(capacityDestinationFilterId, out var capacityDestinationFilter))
				{
					Debug.LogError($"Cannot find filter [ {capacityDestinationFilterId} ] for capacity {capacityDestinationCurrent}");
					continue;
				}
				
				var capacitySourcesAvailable = capacitySources.Values
					.OrderBy(c => c.GetPriority())
					.ToList();

				var capacitySourceSatisfied = false;
				var noRemainingDwellers = false;

				var resourceCapacityTestResults = new Dictionary<long, bool>();
				
				foreach (var capacitySource in capacitySourcesAvailable)
				{
					Context.ReservationInfo reservationSource = null;
					
					var foundResource = capacitySource.GetContainer().TryFindFirst(
						nextResourceItem =>
						{
							if (!resourceCapacityTestResults.TryGetValue(nextResourceItem.Id, out var resourceCapacityTestResult))
							{
								resourceCapacityTestResult = nextResourceItem[Items.Keys.Shared.Type] == Items.Values.Shared.Types.Resource 
								                             && nextResourceItem[Items.Keys.Resource.LogisticState] == Items.Values.Resource.LogisticStates.None
								                             && capacityDestinationFilter.Validate(nextResourceItem);
								
								resourceCapacityTestResults.Add(nextResourceItem.Id, resourceCapacityTestResult);
							}
							
							if (!resourceCapacityTestResult) return false;

							return capacitySource.GetContainer().TryFindFirst(
								nextReservationItem =>
								{
									if (!reservationSources.TryGetValue(nextReservationItem.Id, out reservationSource)) return false;
									if (reservationSource.GetCapacity() != capacitySource) return false;
									if (reservationSource.Item[Items.Keys.Reservation.IsPromised]) return false;
									
									return capacitySource.GetFilter().Validate(nextResourceItem);
								},
								out _
							);
						},
						out var item
					);

					if (!foundResource) continue;
					
					var noValidDwellerNavigations = true;

					dwellers = dwellers
						.OrderBy(m => m.Dweller.DistanceTo(capacitySource.GetParent()))
						.ToList();

					var dwellerIndex = 0;

					void incrementDweller() => dwellerIndex++;
					void popDweller() => dwellers.RemoveAt(dwellerIndex);
					
					while (dwellerIndex < dwellers.Count)
					{
						var dweller = dwellers[dwellerIndex];

						if (!dweller.ValidNavigationTo(capacityDestinationCurrent))
						{
							incrementDweller();
							continue;
						}
						
						noValidDwellerNavigations = false;

						if (!dweller.ValidNavigationTo(capacitySource))
						{
							incrementDweller();
							continue;
						}

						var source = new InventoryPromiseComponent.TransferInfo
						{
							Container = capacitySource.GetContainer(),
							Capacity = capacitySource.Item,
							Reservation = reservationSource.Item
						};
						
						// It's okay if the source doesn't have a capacity pool.
						source.Container
							.TryFindFirst(
								source.Capacity[Items.Keys.Capacity.Pool],
								out source.CapacityPool
							);
						
						var destination = new InventoryPromiseComponent.TransferInfo
						{
							Container = capacityDestinationCurrent.GetContainer(),
							Capacity = capacityDestinationCurrent.Item,
						};
						
						var found = destination.Container
							.TryFindFirst(
								out destination.Reservation,
								(Items.Keys.Shared.Type, Items.Values.Shared.Types.Reservation),
								(Items.Keys.Reservation.IsPromised, false),
								(Items.Keys.Reservation.CapacityId, destination.Capacity.Id),
								(Items.Keys.Reservation.LogisticState, Items.Values.Reservation.LogisticStates.Input)
							);

						if (!found)
						{
							Debug.LogError($"Unable to find valid input reservation for capacity {destination.Capacity.Id} in container {destination.Container.Id}");
							break;
						}

						var destinationCapacityPoolId = destination.Capacity[Items.Keys.Capacity.Pool];
						
						found = destination.Container
							.TryFindFirst(
								destinationCapacityPoolId,
								out destination.CapacityPool
							);

						if (!found)
						{
							Debug.LogError($"Unable to find destination capacity pool [ {destinationCapacityPoolId} ] for reservation {destination.Reservation} in container {source.Container.Id}");
							break;
						}

						capacitySourceSatisfied = dweller.Dweller.InventoryPromises.Transfer(
							item,
							source,
							destination
						); 
						
						popDweller();

						if (capacitySourceSatisfied) break;
					}

					if (capacitySourceSatisfied) break;
					if (noValidDwellerNavigations) break;

					noRemainingDwellers = dwellers.None();
					
					if (noRemainingDwellers) break;
				}
				
				if (noRemainingDwellers) break;
			}

			/*
			var reservationSources = new List<Context.ReservationInfo>();
			var reservationDestinations = new List<Context.ReservationInfo>();

			foreach (var reservation in context.Reservations.Values)
			{
				if (!reservation.Item.TryGet(Items.Keys.Reservation.TargetType, out var targetType))
				{
					Debug.LogError($"Unable to get {Items.Keys.Reservation.TargetType} for reservation {reservation}");
					continue;
				}

				if (targetType == Items.Values.Reservation.TargetTypes.Resource)
				{
					
				}
				else if (targetType == Items.Values.Reservation.TargetTypes.Capacity)
				{
					
				}
				else Debug.LogError($"Unrecognized {Items.Keys.Reservation.TargetType}: {targetType}");
			}
			*/
			
			/*
			var capacityDestinations = new List<Context.CapacityInfo>();
			var capacitySources = new List<Context.CapacityInfo>();
			
			foreach (var capacity in context.Capacities.Values)
			{
				switch (capacity.Calculate())
				{
					case Context.CapacityInfo.Goals.None:
						break;
					case Context.CapacityInfo.Goals.Receive:
						capacityDestinations.Add(capacity);
						break;
					case Context.CapacityInfo.Goals.Distribute:
						capacitySources.Add(capacity);
						break;
					default:
						Debug.LogError($"Unrecognized goal: {capacity.Goal}");
						break;
				}
			}
			
			var capacityPoolForbiddenDestinations = new HashSet<long>();

			foreach (var capacityPool in context.CapacityPools.Values)
			{
				if (!capacityPool.Calculate()) capacityPoolForbiddenDestinations.Add(capacityPool.Item.Id);
			}

			// Order in a way that caches will get filled up or taken from last
			
			capacitySources = capacitySources
				.OrderBy(c => c.Item[Items.Keys.Capacity.IsCache])
				.ThenBy(c => c.GetPriority())
				.ToList();
			
			capacityDestinations = capacityDestinations
				.OrderBy(c => c.Item[Items.Keys.Capacity.IsCache])
				.ThenBy(c => c.GetPriority())
				.ToList();

			var dwellers = context.Dwellers
				.Where(m => m.Dweller.InventoryPromises.All.None())
				.ToList();
			
			// While loop below is entirely for handling assignment of transfers to dwellers.
			while (capacitySources.Any() && capacityDestinations.Any() && dwellers.Any())
			{
				var capacityDestinationCurrent = capacityDestinations[0];
				capacityDestinations.RemoveAt(0);

				if (capacityPoolForbiddenDestinations.Contains(capacityDestinationCurrent.Item[Items.Keys.Capacity.Pool])) continue;
				
				var resourceType = capacityDestinationCurrent.Item[Items.Keys.Capacity.ResourceType];

				var capacitySourcesAvailable = capacitySources
					.Where(c => c.Item[Items.Keys.Capacity.ResourceType] == resourceType)
					.ToList();

				var capacitySourceSatisfied = false;
				var noRemainingDwellers = false;
				
				foreach (var capacitySource in capacitySourcesAvailable)
				{
					var noValidDwellerNavigations = true;

					dwellers = dwellers
						.OrderBy(m => m.Dweller.DistanceTo(capacitySource.GetParent()))
						.ToList();

					var dwellerIndex = 0;

					void incrementDweller() => dwellerIndex++;
					void popDweller() => dwellers.RemoveAt(dwellerIndex);
					
					while (dwellerIndex < dwellers.Count)
					{
						var dweller = dwellers[dwellerIndex];

						if (!dweller.ValidNavigationTo(capacityDestinationCurrent))
						{
							incrementDweller();
							continue;
						}
						
						noValidDwellerNavigations = false;

						if (!dweller.ValidNavigationTo(capacitySource))
						{
							incrementDweller();
							continue;
						}

						var source = new InventoryPromiseComponent.TransferInfo
						{
							Container = capacitySource.GetContainer(),
							Capacity = capacitySource.Item
						};
						
						var found = source.Container
							.TryFindFirst(
								out source.Reservation,
								(Items.Keys.Shared.Type, Items.Values.Shared.Types.Reservation),
								(Items.Keys.Reservation.IsPromised, false),
								(Items.Keys.Reservation.CapacityId, source.Capacity.Id),
								(Items.Keys.Reservation.LogisticState, Items.Values.Reservation.LogisticStates.Output)
							);

						if (!found)
						{
							// This will occur if there is some other incoming reservation or whatnot, may not happen...
							incrementDweller();
							continue;
						}

						// It's okay if the source doesn't have a capacity pool.
						source.Container
							.TryFindFirst(
								source.Capacity[Items.Keys.Capacity.Pool],
								out source.CapacityPool
							);

						found = source.Container
							.TryFindFirst(
								out var item,
								(Items.Keys.Shared.Type, Items.Values.Shared.Types.Resource),
								(Items.Keys.Resource.LogisticState, Items.Values.Resource.LogisticStates.None),
								(Items.Keys.Resource.Type, resourceType)
							);

						if (!found)
						{
							Debug.LogError($"Unable to find valid instance of a {resourceType} in container {source.Container.Id}");
							break;
						}
						
						var destination = new InventoryPromiseComponent.TransferInfo
						{
							Container = capacityDestinationCurrent.GetContainer(),
							Capacity = capacityDestinationCurrent.Item,
						};
						
						found = destination.Container
							.TryFindFirst(
								out destination.Reservation,
								(Items.Keys.Shared.Type, Items.Values.Shared.Types.Reservation),
								(Items.Keys.Reservation.IsPromised, false),
								(Items.Keys.Reservation.CapacityId, destination.Capacity.Id),
								(Items.Keys.Reservation.LogisticState, Items.Values.Reservation.LogisticStates.Input)
							);

						if (!found)
						{
							Debug.LogError($"Unable to find valid input reservation for {destination.Reservation} in container {source.Container.Id}");
							break;
						}

						var destinationCapacityPoolId = destination.Capacity[Items.Keys.Capacity.Pool];
						
						found = destination.Container
							.TryFindFirst(
								destinationCapacityPoolId,
								out destination.CapacityPool
							);

						if (!found)
						{
							Debug.LogError($"Unable to find destination capacity pool [ {destinationCapacityPoolId} ] for reservation {destination.Reservation} in container {source.Container.Id}");
							break;
						}

						capacitySourceSatisfied = dweller.Dweller.InventoryPromises.Transfer(
							item,
							source,
							destination
						); 
						
						popDweller();

						if (capacitySourceSatisfied) break;
					}

					if (capacitySourceSatisfied) break;
					if (noValidDwellerNavigations) break;

					noRemainingDwellers = dwellers.None();
					
					if (noRemainingDwellers) break;
				}
				
				if (noRemainingDwellers) break;
			}
			*/

			context.Clear();
		}
		
		void OnItemUpdate(Item item)
		{
			// Since cleanup is handled by the ProcessorStore, which may happen before or after this service is called,
			// we ignore items with no instances...
			if (item.NoInstances) return;
			
			var type = item[Items.Keys.Shared.Type];

			if (type == Items.Values.Shared.Types.Resource)
			{
				context.Resources.Add(
					item.Id,
					new Context.ResourceInfo(
						context,
						item
					)
				);
			}
			else if (type == Items.Values.Shared.Types.Reservation)
			{
				context.Reservations.Add(
					item.Id,
					new Context.ReservationInfo(
						context,
						item
					)
				);
			}
			else if (type == Items.Values.Shared.Types.CapacityPool)
			{
				context.CapacityPools.Add(
					item.Id,
					new Context.CapacityPoolInfo(
						context,
						item
					)
				);
			}
			else if (type == Items.Values.Shared.Types.Capacity)
			{
				context.Capacities.Add(
					item.Id,
					new Context.CapacityInfo(
						context,
						item
					)
				);
			}
		}
		#endregion
	}
}