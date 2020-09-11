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

				public abstract Container GetInventory();

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

				public override Container GetInventory() => Dweller.Inventory.Container;
				public override IInventoryModel GetParent() => Dweller;
			}
			
			public abstract class ItemInfo : Info
			{
				public Item Item { get; }

				bool? inventoryFound;
				bool? parentFound;
				string resourceType;
				
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
				
				public override Container GetInventory()
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

				public string GetResourceType() => resourceType ?? (resourceType = OnGetResourceType());
				
				protected abstract string OnGetResourceType();
			}
			
			public class ResourceInfo : ItemInfo
			{
				public ResourceInfo(
					Context context,
					Item item
				) : base(context, item) { }

				protected override string OnGetResourceType() => Item[Items.Keys.Resource.Type];
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
				
				public CapacityInfo(
					Context context,
					Item item
				) : base(context, item) { }

				protected override string OnGetResourceType() => Item[Items.Keys.Capacity.ResourceType];
				
				public Goals Calculate()
				{
					var poolId = Item[Items.Keys.Capacity.Pool];

					int? poolCapacity = null;
					
					if (poolId != IdCounter.UndefinedId)
					{
						if (Context.service.Model.Items.TryGet(poolId, out var poolItem))
						{
							poolCapacity = Mathf.Max(0, poolItem[Items.Keys.CapacityPool.CountMaximum] - poolItem[Items.Keys.CapacityPool.CountCurrent]);
						}
						else Debug.LogError($"Cannot find capacity pool with id [ {poolId} ]");
					}
					
					var desire = Item[Items.Keys.Capacity.Desire];
					if (desire != Items.Values.Capacity.Desires.NotCalculated)
					{
						if (desire == Items.Values.Capacity.Desires.None) return Goal = Goals.None;
						if (desire == Items.Values.Capacity.Desires.Receive) return Goal = Goals.Receive;
						if (desire == Items.Values.Capacity.Desires.Distribute) return Goal = Goals.Distribute;
						Debug.LogError($"Unrecognized desire: {desire}");
					}

					var resourceType = GetResourceType();
					var capacityTargetCount = Item[Items.Keys.Capacity.CountTarget];
				
					var inventory = GetInventory();

					var resourceTotalCount = 0;

					foreach (var stack in inventory.Stacks)
					{
						if (!Context.Resources.TryGetValue(stack.Id, out var resource)) continue;
						if (resource.Item[Items.Keys.Resource.Type] != resourceType) continue;
						// TODO: I probably just shouldn't add ones note equal to None?
						if (resource.Item[Items.Keys.Resource.LogisticState] != Items.Values.Resource.LogisticStates.None) continue;
						
						resourceTotalCount += stack.Count;
					}

					var delta = capacityTargetCount - resourceTotalCount;
					if (poolCapacity.HasValue) delta = Mathf.Min(delta, poolCapacity.Value);
				
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
										Item.Id
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
									Item.Id
								)
							)
							.Done(Mathf.Abs(delta))
					);
					
					return Goal = Goals.Distribute;
				}
			}

			LogisticsService service;

			public List<NavigationCache> NavigationCaches = new List<NavigationCache>();
			public List<DwellerInfo> Dwellers = new List<DwellerInfo>();
			public Dictionary<long, ResourceInfo> Resources = new Dictionary<long, ResourceInfo>();
			public Dictionary<long, CapacityInfo> Capacities = new Dictionary<long, CapacityInfo>();
			public Dictionary<long, Container> Inventories = new Dictionary<long, Container>();
			public Dictionary<long, IInventoryModel> Parents = new Dictionary<long, IInventoryModel>();

			public Context(LogisticsService service) => this.service = service;
			
			public void Clear()
			{
				NavigationCaches.Clear();
				Dwellers.Clear();
				Resources.Clear();
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

			var capacitiesReceive = new List<Context.CapacityInfo>();
			var capacitiesDistribute = new List<Context.CapacityInfo>();
			
			foreach (var capacity in context.Capacities.Values)
			{
				switch (capacity.Calculate())
				{
					case Context.CapacityInfo.Goals.None:
						break;
					case Context.CapacityInfo.Goals.Receive:
						capacitiesReceive.Add(capacity);
						break;
					case Context.CapacityInfo.Goals.Distribute:
						capacitiesDistribute.Add(capacity);
						break;
					default:
						Debug.LogError($"Unrecognized goal: {capacity.Goal}");
						break;
				}
			}

			// Order in a way that cachecs will get filled up or taken from last
			
			capacitiesReceive = capacitiesReceive
				.OrderBy(c => c.Item[Items.Keys.Capacity.IsCache])
				.ThenBy(c => c.GetPriority())
				.ToList();

			capacitiesDistribute = capacitiesDistribute
				.OrderBy(c => c.Item[Items.Keys.Capacity.IsCache])
				.ThenBy(c => c.GetPriority())
				.ToList();

			var dwellersAvailable = context.Dwellers
				.Where(m => m.Dweller.InventoryPromises.All.None())
				.ToList();
			
			// While loop below is entirely for handling assignment of transfers to dwellers.
			while (capacitiesReceive.Any() && capacitiesDistribute.Any() && dwellersAvailable.Any())
			{
				var capacityReceive = capacitiesReceive[0];
				capacitiesReceive.RemoveAt(0);

				var resourceType = capacityReceive.Item[Items.Keys.Capacity.ResourceType];

				var capacitiesDistributeAvailable = capacitiesDistribute
					.Where(c => c.Item[Items.Keys.Capacity.ResourceType] == resourceType)
					.ToList();

				var capacityReceiveFulfilled = false;
				var capacitiesDistributeConsumed = new List<Context.CapacityInfo>();
				
				foreach (var capacityDistribute in capacitiesDistributeAvailable)
				{
					var noValidDwellerNavigations = true;
					Context.DwellerInfo dwellerAssigned = null;
					
					foreach (var dweller in dwellersAvailable.OrderBy(m => m.Dweller.DistanceTo(capacityDistribute.GetParent())))
					{
						if (!dweller.ValidNavigationTo(capacityReceive)) continue;
						noValidDwellerNavigations = false;
						
						if (!dweller.ValidNavigationTo(capacityDistribute)) continue;

						var inventoryDistribute = capacityDistribute.GetInventory();

						var found = inventoryDistribute
							.TryFindFirst(
								out var itemReservationDistribute,
								(Items.Keys.Shared.Type, Items.Values.Shared.Types.Reservation),
								(Items.Keys.Reservation.IsPromised, false),
								(Items.Keys.Reservation.CapacityId, capacityDistribute.Item.Id),
								(Items.Keys.Reservation.LogisticState, Items.Values.Reservation.LogisticStates.Output)
							);

						if (!found)
						{
							// This will occur if there is some other incoming reservation or whatnot, may not happen...
							continue;
						}
						
						found = inventoryDistribute
							.TryFindFirst(
								out var item,
								(Items.Keys.Shared.Type, Items.Values.Shared.Types.Resource),
								(Items.Keys.Resource.LogisticState, Items.Values.Resource.LogisticStates.None),
								(Items.Keys.Resource.Type, resourceType)
							);

						if (!found)
						{
							Debug.LogError($"Unable to find valid instance of a {resourceType} in {inventoryDistribute.Id}");
							break;
						}

						item = Model.Items
							.First(
								inventoryDistribute
									.Withdrawal(
										item.StackOf(1)
									).First()
							);
						
						itemReservationDistribute = Model.Items
							.First(
								inventoryDistribute
									.Withdrawal(
										itemReservationDistribute.StackOf(1)
									).First()
							);
						
						item[Items.Keys.Resource.LogisticState] = Items.Values.Resource.LogisticStates.Output;

						itemReservationDistribute[Items.Keys.Reservation.IsPromised] = true;

						inventoryDistribute.Deposit(item.StackOf(1));
						inventoryDistribute.Deposit(itemReservationDistribute.StackOf(1));

						capacityReceive.Item[Items.Keys.Capacity.CountCurrent]++;

						capacityReceiveFulfilled = capacityReceive.Item[Items.Keys.Capacity.CountCurrent] == capacityReceive.Item[Items.Keys.Capacity.CountTarget];

						if (capacityReceiveFulfilled) capacityReceive.Item[Items.Keys.Capacity.Desire] = Items.Values.Capacity.Desires.None;
						
						var inventoryReceive = capacityReceive.GetInventory();

						found = inventoryReceive
							.TryFindFirst(
								out var itemReservationReceive,
								(Items.Keys.Shared.Type, Items.Values.Shared.Types.Reservation),
								(Items.Keys.Reservation.IsPromised, false),
								(Items.Keys.Reservation.CapacityId, capacityReceive.Item.Id),
								(Items.Keys.Reservation.LogisticState, Items.Values.Reservation.LogisticStates.Input)
							);

						if (!found)
						{
							Debug.LogError($"Unable to find valid input reservation for {capacityReceive.Item.Id} in {inventoryDistribute.Id}");
							break;
						}
						
						itemReservationReceive = Model.Items
							.First(
								inventoryReceive
								.Withdrawal(
									itemReservationReceive.StackOf(1)
								).First()
							);
						
						itemReservationReceive[Items.Keys.Reservation.IsPromised] = true;

						inventoryReceive.Deposit(itemReservationReceive.StackOf(1));
						
						var dwellerInventory = dweller.GetInventory();
						
						dwellerInventory.Deposit(
							Model.Items.Builder
								.BeginItem()
								.WithProperties(
									Items.Instantiate.Transfer.Pickup(
										item.Id,
										itemReservationDistribute.Id,
										itemReservationReceive.Id
									)	
								)
								.Done(1, out var transfer)
						);

						itemReservationDistribute[Items.Keys.Reservation.TransferId] = transfer.Id;
						itemReservationReceive[Items.Keys.Reservation.TransferId] = transfer.Id;
							
						dweller.Dweller.InventoryPromises.All.Push(transfer.Id);

						dwellerAssigned = dweller;
						break;
					}

					if (capacityReceiveFulfilled) break;
					if (noValidDwellerNavigations) break;

					Debug.Log("count: "+dwellersAvailable.Count);
					if (dwellerAssigned != null) dwellersAvailable.Remove(dwellerAssigned);

					foreach (var consumed in capacitiesDistributeConsumed) capacitiesDistribute.Remove(consumed);
				}
			}

			context.Clear();
		}

		void OnItemUpdate(Item item)
		{
			var type = item[Items.Keys.Shared.Type];
			
			if (type == Items.Values.Shared.Types.Resource) OnResourceUpdate(item);
			else if (type == Items.Values.Shared.Types.Capacity) OnCapacityUpdate(item);
			
			// if (Model.SimulationTick.Value < item.Get(Items.Keys.Capacity.TimeoutExpired)) return;
			// item.Set(Items.Keys.Capacity.TimeoutExpired, Model.SimulationTick.Value + 120L);
		}

		void OnResourceUpdate(Item item)
		{
			context.Resources.Add(
				item.Id,
				new Context.ResourceInfo(
					context,
					item
				)
			);
		}

		void OnCapacityUpdate(Item item)
		{
			context.Capacities.Add(
				item.Id,
				new Context.CapacityInfo(
					context,
					item
				)
			);
		}
		#endregion
	}
}