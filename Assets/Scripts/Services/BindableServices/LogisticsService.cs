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

				public abstract Inventory GetInventory();

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

				public override Inventory GetInventory() => Dweller.Inventory.Container;
				public override IInventoryModel GetParent() => Dweller;
			}
			
			public abstract class ItemInfo : Info
			{
				public Item Item { get; }

				bool? inventoryFound;
				bool? parentFound;
				string resourceId;
				
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
				
				public override Inventory GetInventory()
				{
					if (inventoryFound.HasValue)
					{
						if (inventoryFound.Value) return Context.Inventories[Item.InventoryId];
						return null;
					}

					var parent = GetParent(); 
					
					if (parent == null)
					{
						inventoryFound = false;
						return null;
					}

					inventoryFound = true;
					Context.Inventories.Add(Item.InventoryId, parent.Inventory.Container);
					return parent.Inventory.Container;
				}

				public override IInventoryModel GetParent()
				{
					if (parentFound.HasValue)
					{
						if (parentFound.Value) return Context.Parents[Item.InventoryId];
						return null;
					}

					var parent = Context.service.Model.Query
						.FirstOrDefault<IInventoryModel>(i => i.Inventory.Container.Id == Item.InventoryId);

					parentFound = parent != null;
					if (parentFound.Value) Context.Parents.Add(Item.InventoryId, parent);
					
					return parent;
				}

				public string GetResourceId() => resourceId ?? (resourceId = OnGetResourceId());
				
				protected abstract string OnGetResourceId();
			}
			
			public class ResourceInfo : ItemInfo
			{
				public ResourceInfo(
					Context context,
					Item item
				) : base(context, item) { }

				protected override string OnGetResourceId() => Item.Get(Items.Keys.Resource.Id);
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

				protected override string OnGetResourceId() => Item.Get(Items.Keys.Capacity.ResourceId);
				
				public Goals Calculate()
				{
					var desire = Item.Get(Items.Keys.Capacity.Desire);
					if (desire != Items.Values.Capacity.Desires.NotCalculated)
					{
						if (desire == Items.Values.Capacity.Desires.None) return Goal = Goals.None;
						if (desire == Items.Values.Capacity.Desires.Receive) return Goal = Goals.Receive;
						if (desire == Items.Values.Capacity.Desires.Distribute) return Goal = Goals.Distribute;
						Debug.LogError($"Unrecognized desire: {desire}");
					}

					var resourceId = GetResourceId();
					// var capacityCurrentCount = capacity.Item.Get(Items.Keys.Capacity.CurrentCount);
					// var capacityMaximumCount = capacity.Item.Get(Items.Keys.Capacity.MaximumCount);
					var capacityTargetCount = Item.Get(Items.Keys.Capacity.TargetCount);
				
					var inventory = GetInventory();

					var resourceTotalCount = 0;

					foreach (var stack in inventory.Stacks)
					{
						if (!Context.Resources.TryGetValue(stack.Id, out var resource)) continue;
						if (resource.Item.Get(Items.Keys.Resource.Id) != resourceId) continue;
						// TODO: I probably just shouldn't add ones note equal to None?
						if (resource.Item.Get(Items.Keys.Resource.Logistics.State) != Items.Values.Resource.Logistics.States.None) continue;
						
						resourceTotalCount += stack.Count;
					}

					var delta = capacityTargetCount - resourceTotalCount;
				
					if (delta == 0)
					{
						// We're at a satisfactory amount
						Item.Set(
							Items.Keys.Capacity.Desire.Pair(Items.Values.Capacity.Desires.None),
							Items.Keys.Capacity.CurrentCount.Pair(resourceTotalCount)
						);

						return Goal = Goals.None;
					}
					
					if (0 < delta)
					{
						// We want more
						Item.Set(
							Items.Keys.Capacity.Desire.Pair(Items.Values.Capacity.Desires.Receive),
							Items.Keys.Capacity.CurrentCount.Pair(resourceTotalCount)
						);

						inventory.Deposit(
							Context.service.Model.Items.Builder
								.BeginItem()
								.WithProperties(
									Items.Instantiate.Reservation.OfInput(
										resourceId,
										Item.Id
									)
								)
								.Done(delta)
						);
						
						return Goal = Goals.Receive;
					}
					
					// We want less
					Item.Set(
						Items.Keys.Capacity.Desire.Pair(Items.Values.Capacity.Desires.Distribute),
						Items.Keys.Capacity.CurrentCount.Pair(resourceTotalCount)
					);
				
					inventory.Deposit(
						Context.service.Model.Items.Builder
							.BeginItem()
							.WithProperties(
								Items.Instantiate.Reservation.OfOutput(
									resourceId,
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
			public Dictionary<long, Inventory> Inventories = new Dictionary<long, Inventory>();
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
			
			Model.Items.IterateAll(OnItemUpdate);

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
				.OrderBy(c => c.Item.Get(Items.Keys.Capacity.IsCache))
				.ThenBy(c => c.GetPriority())
				.ToList();

			capacitiesDistribute = capacitiesDistribute
				.OrderBy(c => c.Item.Get(Items.Keys.Capacity.IsCache))
				.ThenBy(c => c.GetPriority())
				.ToList();

			var availableDwellers = context.Dwellers
				.Where(m => m.Dweller.InventoryPromises.All.None())
				.ToList();
			
			while (capacitiesReceive.Any() && capacitiesDistribute.Any() && availableDwellers.Any())
			{
				var nextRecieve = capacitiesReceive[0];
				capacitiesReceive.RemoveAt(0);

				var resourceId = nextRecieve.Item.Get(Items.Keys.Capacity.ResourceId);

				var availableDistributions = capacitiesDistribute
					.Where(c => c.Item.Get(Items.Keys.Capacity.ResourceId) == resourceId)
					.ToList();

				foreach (var availableDistribution in availableDistributions)
				{
					var noValidDwellerNavigations = true;
					
					foreach (var dweller in availableDwellers.OrderBy(m => m.Dweller.DistanceTo(availableDistribution.GetParent())))
					{
						if (!dweller.ValidNavigationTo(nextRecieve)) continue;
						noValidDwellerNavigations = false;
						
						if (!dweller.ValidNavigationTo(availableDistribution)) continue;
						
						// var distributionInventory = availableDistribution.GetInventory();
						// var receiveInventory = nextRecieve.GetInventory();
						// var dwellerInventory = dweller.GetInventory();
					}

					if (noValidDwellerNavigations) break;
				}
			}

			context.Clear();
		}

		void OnItemUpdate(Item item)
		{
			var type = item.Get(Items.Keys.Shared.Type);
			
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