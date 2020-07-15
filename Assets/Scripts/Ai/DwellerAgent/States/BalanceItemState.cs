using System.Collections.Generic;
using System.Linq;
using Lunra.Hothouse.Ai.Dweller;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public class BalanceItemState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "BalanceItems";
		
		public override void OnInitialize()
		{
			AddChildStates(
				new InventoryRequestState<BalanceItemState<S>>()	
			);
		
			AddTransitions(
				new InventoryRequestState<BalanceItemState<S>>.ToInventoryRequestOnPromises(),
				new ToReturnOnFallthrough()
			);
		}

		public abstract class ToBalanceOnAvailable : AgentTransition<S, BalanceItemState<S>, GameModel, DwellerModel>
		{
			protected class EnterableCache
			{
				public IEnterableModel Model;
				public Navigation.Result NavigationResult;
				public bool? IsNavigable;
			}

			protected class InventoryCache
			{
				public EnterableCache Enterable;
				public InventoryComponent Inventory;
				public bool CanDeposit;
				public bool CanWithdrawal;
				
				public bool IsRequestingDelivery;
				public bool IsRequestingDistribution;
			}
			
			protected enum Actions
			{
				Unknown = 0,
				Delivery = 10,
				Distribution = 20
			}

			protected abstract Actions CurrentAction { get; }
			
			protected List<EnterableCache> EnterablesCached = new List<EnterableCache>();
			protected List<InventoryCache> InventoriesCached = new List<InventoryCache>();
			
			public override bool IsTriggered()
			{
				EnterablesCached.Clear();
				InventoriesCached.Clear();

				var anyDeliveries = false;
				var anyDistributions = false;
				
				foreach (var parent in Game.GetInventoryParents())
				{
					switch (parent)
					{
						case IEnterableModel enterableModel:
							if (!enterableModel.Enterable.AnyAvailable()) continue;
							break;
						default:
							continue;
					}
					
					if (!(parent is IInventoryModel parentTyped)) continue;
					
					var enterableEntry = new EnterableCache();
					enterableEntry.Model = parentTyped;
					
					var anyInventoriesReferenced = false;

					foreach (var baseInventory in parentTyped.Inventories)
					{
						if (!(baseInventory is InventoryComponent inventory)) continue;

						var inventoryEntry = new InventoryCache();

						inventoryEntry.Enterable = enterableEntry;
						inventoryEntry.Inventory = inventory;
						inventoryEntry.CanDeposit = inventory.Permission.Value.CanDeposit(Agent);
						inventoryEntry.CanWithdrawal = inventory.Permission.Value.CanWithdrawal(Agent);
					
						if (!inventoryEntry.CanDeposit && !inventoryEntry.CanWithdrawal) continue;

						inventoryEntry.IsRequestingDelivery = !inventory.Desired.Value.Delivery.IsEmpty && inventoryEntry.CanDeposit;
						inventoryEntry.IsRequestingDistribution = !inventory.Desired.Value.Distribution.IsEmpty && inventoryEntry.CanWithdrawal;
					
						InventoriesCached.Add(inventoryEntry);
						
						anyDeliveries |= inventoryEntry.IsRequestingDelivery;
						anyDistributions |= inventoryEntry.IsRequestingDistribution;

						anyInventoriesReferenced = true;
					}
					
					if (anyInventoriesReferenced) EnterablesCached.Add(enterableEntry);
				}

				switch (CurrentAction)
				{
					case Actions.Delivery:
						if (!anyDeliveries) return false;
						return IsActionTriggered();
					case Actions.Distribution:
						if (!anyDistributions) return false;
						return IsActionTriggered();
					default:
						Debug.LogError("Unrecognized Action: " + CurrentAction);
						return false;
				}
			}

			protected abstract bool IsActionTriggered();
			
			protected bool GetIsNavigable(EnterableCache enterableCache)
			{
				if (!enterableCache.IsNavigable.HasValue)
				{
					enterableCache.IsNavigable = NavigationUtility.CalculateNearest(
						Agent.Transform.Position.Value,
						out enterableCache.NavigationResult,
						Navigation.QueryEntrances(enterableCache.Model)
					);
				}

				return enterableCache.IsNavigable.Value;
			}
			
			protected bool TryGetItems(
				Inventory inventoryToTransfer,
				out Inventory inventory
			)
			{
				return Agent.Inventory.AllCapacity.Value.GetCapacityFor(Agent.Inventory.All.Value)
					.Intersects(inventoryToTransfer, out inventory);
			}

			// It may not be necessary to break these out into the virtual methods below, but it does give the option of
			// overriding how ordering is done in the future...

			protected virtual float OnOrderByDistanceToAgent(InventoryCache inventoryCache)
			{
				return inventoryCache.Enterable.Model.DistanceTo(Agent);
			}
			
			protected virtual float OnOrderByDistance(
				InventoryCache inventoryCache0,
				InventoryCache inventoryCache1
			)
			{
				return inventoryCache0.Enterable.Model.DistanceTo(inventoryCache1.Enterable.Model);
			}
		}

		public class ToBalanceOnAvailableDelivery : ToBalanceOnAvailable
		{
			protected override Actions CurrentAction => Actions.Delivery;

			InventoryCache deliveryTarget;
			InventoryCache deliverySource;
			Inventory items;
			
			protected override bool IsActionTriggered()
			{
				var possibleDeliveryTargets = InventoriesCached
					.Where(m => m.CanDeposit && m.IsRequestingDelivery)
					.OrderBy(OnOrderByDistanceToAgent);

				foreach (var possibleDeliveryTarget in possibleDeliveryTargets)
				{
					if (!GetIsNavigable(possibleDeliveryTarget.Enterable)) continue;

					var possibleDeliverySources = InventoriesCached
						.Where(m => m.CanWithdrawal)
						.OrderBy(m => OnOrderByDistance(m, possibleDeliveryTarget));

					foreach (var possibleDeliverySource in possibleDeliverySources)
					{
						if (!GetIsNavigable(possibleDeliverySource.Enterable)) continue;

						var isIntersecting = possibleDeliveryTarget.Inventory.Desired.Value.Delivery.Intersects(
							possibleDeliverySource.Inventory.Available.Value,
							out var intersection
						);
						
						if (!isIntersecting) continue;

						if (!TryGetItems(intersection, out items)) continue;
						
						deliveryTarget = possibleDeliveryTarget;
						deliverySource = possibleDeliverySource;
						return true;
					}
				}

				return false;
			}

			public override void Transition()
			{
				Agent.InventoryPromises.Push(
					items,
					deliverySource.Inventory,
					deliveryTarget.Inventory
				);
			}
		}
		
		public class ToBalanceOnAvailableDistribution : ToBalanceOnAvailable
		{
			protected override Actions CurrentAction => Actions.Distribution;
			
			InventoryCache distributionSource;
			InventoryCache distributionDestination;
			Inventory items;
			
			protected override bool IsActionTriggered()
			{
				var possibleDistributionSources = InventoriesCached
					.Where(m => m.CanWithdrawal && m.IsRequestingDistribution)
					.OrderBy(OnOrderByDistanceToAgent);

				foreach (var possibleDistributionSource in possibleDistributionSources)
				{
					if (!GetIsNavigable(possibleDistributionSource.Enterable)) continue;

					var possibleDistributionDestinations = InventoriesCached
						.Where(m => m.CanDeposit)
						.OrderBy(m => OnOrderByDistance(m, possibleDistributionSource));

					foreach (var possibleDistributionDestination in possibleDistributionDestinations)
					{
						if (!GetIsNavigable(possibleDistributionDestination.Enterable)) continue;

						var isIntersecting = possibleDistributionSource.Inventory.Desired.Value.Distribution.Intersects(
							possibleDistributionDestination.Inventory.AvailableCapacity.Value.GetCapacityFor(
								possibleDistributionDestination.Inventory.Available.Value	
							),
							out var intersection
						);
						
						if (!isIntersecting) continue;
						
						if (!TryGetItems(intersection, out items)) continue;

						distributionSource = possibleDistributionSource;
						distributionDestination = possibleDistributionDestination;
						return true;
					}
				}

				return false;
			}

			public override void Transition()
			{
				Agent.InventoryPromises.Push(
					items,
					distributionSource.Inventory,
					distributionDestination.Inventory
				);
			}
		}

		class ToReturnOnFallthrough : AgentTransition<BalanceItemState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => true;
		}		
	}
}