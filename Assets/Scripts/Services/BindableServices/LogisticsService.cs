using System;
using System.Collections.Generic;
using Lunra.Hothouse.Models;
using Lunra.Satchel;
using Lunra.StyxMvp.Services;
using UnityEngine;

namespace Lunra.Hothouse.Services
{
	public class LogisticsService : BindableService<GameModel>
	{
		class Context
		{
			public abstract class ItemInfo
			{
				public Item Item { get; }
				protected Context Context { get; }

				bool? inventoryFound;
				bool? parentFound;
				
				public ItemInfo(
					Context context,
					Item item
				)
				{
					Item = item;

					Context = context;
				}

				public Inventory GetInventory()
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
					Context.Inventories.Add(Item.InventoryId, parent.Container);
					return parent.Container;
				}

				public InventoryComponent GetParent()
				{
					if (parentFound.HasValue)
					{
						if (parentFound.Value) return Context.Parents[Item.InventoryId];
						return null;
					}
					
					var parent = Context.service.Model.Query
						.FirstOrDefault<IInventoryModel>(i => i.Inventory.Container.Id == Item.InventoryId)
						?.Inventory;

					parentFound = parent != null;
					if (parentFound.Value) Context.Parents.Add(Item.InventoryId, parent);
					
					return parent;
				}
			}
			
			public class ResourceInfo : ItemInfo
			{
				public ResourceInfo(
					Context context,
					Item item
				) : base(context, item)
				{
					
				}
			}

			public class CapacityInfo : ItemInfo
			{
				public CapacityInfo(
					Context context,
					Item item
				) : base(context, item) { }
			}

			LogisticsService service;

			public Dictionary<long, ResourceInfo> Resources = new Dictionary<long, ResourceInfo>();
			public Dictionary<long, CapacityInfo> Capacities = new Dictionary<long, CapacityInfo>();
			public Dictionary<long, Inventory> Inventories = new Dictionary<long, Inventory>();
			public Dictionary<long, InventoryComponent> Parents = new Dictionary<long, InventoryComponent>();

			public Context(LogisticsService service) => this.service = service;
			
			public void Clear()
			{
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
			Model.Items.IterateAll(OnItemUpdate);

			foreach (var capacity in context.Capacities.Values)
			{
				if (capacity.Item.Get(Items.Keys.Capacity.Desire) != Items.Values.Capacity.Desires.NotCalculated) continue;

				var capacityResourceId = capacity.Item.Get(Items.Keys.Capacity.ResourceId);
				// var capacityCurrentCount = capacity.Item.Get(Items.Keys.Capacity.CurrentCount);
				// var capacityMaximumCount = capacity.Item.Get(Items.Keys.Capacity.MaximumCount);
				var capacityTargetCount = capacity.Item.Get(Items.Keys.Capacity.TargetCount);
				
				var inventory = capacity.GetInventory();

				var resourceTotalCount = 0;

				foreach (var stack in inventory.Stacks)
				{
					if (stack.Is(capacity.Item)) continue;

					if (Model.Items.TryGet(stack.Id, out var possibleResource))
					{
						if (!possibleResource.TryGet(Items.Keys.Resource.Id, out var possibleResourceId)) continue;
						if (possibleResourceId != capacityResourceId) continue;
						if (possibleResource.Get(Items.Keys.Resource.Logistics.State) != Items.Values.Resource.Logistics.States.None) continue;
						
						resourceTotalCount += stack.Count;
					}
				}
				
				var delta = capacityTargetCount - resourceTotalCount;
				
				if (delta == 0)
				{
					capacity.Item.Set(
						Items.Keys.Capacity.Desire.Pair(Items.Values.Capacity.Desires.None),
						Items.Keys.Capacity.CurrentCount.Pair(resourceTotalCount)
					);
				}
				else if (0 < delta)
				{
					// We want more
					capacity.Item.Set(
						Items.Keys.Capacity.Desire.Pair(Items.Values.Capacity.Desires.Fulfill),
						Items.Keys.Capacity.CurrentCount.Pair(resourceTotalCount)
					);

					inventory.Deposit(
						Model.Items.Builder
							.BeginItem()
							.WithProperties(
								Items.Instantiate.Reservation.OfInput(
									capacityResourceId,
									capacity.Item.Id
								)
							)
							.Done(delta)
					);
				}
				else
				{
					// We want less
					capacity.Item.Set(
						Items.Keys.Capacity.Desire.Pair(Items.Values.Capacity.Desires.Distribute),
						Items.Keys.Capacity.CurrentCount.Pair(resourceTotalCount)
					);
					
					inventory.Deposit(
						Model.Items.Builder
							.BeginItem()
							.WithProperties(
								Items.Instantiate.Reservation.OfOutput(
									capacityResourceId,
									capacity.Item.Id
								)
							)
							.Done(Mathf.Abs(delta))
					);
				}
			}

			// TODO: Sort these by some priority
			var sortedResources = context.Resources;
			var sortedCapacities = context.Capacities;

			var dwellerPool = new Dictionary<string, DwellerModel>();

			foreach (var dweller in Model.Dwellers.AllActive)
			{
				// if (dwe)
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