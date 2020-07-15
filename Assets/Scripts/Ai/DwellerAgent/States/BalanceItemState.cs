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
			protected class Cache
			{
				public BuildingModel Model;
				public bool CanDeposit;
				public bool CanWithdrawal;
				public bool? IsNavigable;
				public Navigation.Result NavigationResult;

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
			
			protected List<Cache> Cached = new List<Cache>();
			
			public override bool IsTriggered()
			{
				Cached.Clear();

				var anyDeliveries = false;
				var anyDistributions = false;
				
				foreach (var model in Game.Buildings.AllActive)
				{
					if (!model.Enterable.AnyAvailable()) continue;

					var entry = new Cache();

					entry.Model = model;
					entry.CanDeposit = model.Inventory.Permission.Value.CanDeposit(Agent);
					entry.CanWithdrawal = model.Inventory.Permission.Value.CanWithdrawal(Agent);
					
					if (!entry.CanDeposit && !entry.CanWithdrawal) continue;

					entry.IsRequestingDelivery = !model.Inventory.Desired.Value.Delivery.IsEmpty && entry.CanDeposit;
					entry.IsRequestingDistribution = !model.Inventory.Desired.Value.Distribution.IsEmpty && entry.CanWithdrawal;
					
					Cached.Add(entry);

					anyDeliveries |= entry.IsRequestingDelivery;
					anyDistributions |= entry.IsRequestingDistribution;
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
			
			protected bool GetIsNavigable(Cache cache)
			{
				if (!cache.IsNavigable.HasValue)
				{
					cache.IsNavigable = NavigationUtility.CalculateNearest(
						Agent.Transform.Position.Value,
						out cache.NavigationResult,
						Navigation.QueryEntrances(cache.Model)
					);
				}

				return cache.IsNavigable.Value;
			}
			
			protected bool TryGetItems(
				Inventory inventoryToTransfer,
				out Inventory inventory
			)
			{
				return Agent.Inventory.AllCapacity.Value.GetCapacityFor(Agent.Inventory.All.Value)
					.Intersects(inventoryToTransfer, out inventory);
			}
		}

		public class ToBalanceOnAvailableDelivery : ToBalanceOnAvailable
		{
			protected override Actions CurrentAction => Actions.Delivery;

			Cache deliveryTarget;
			Cache deliverySource;
			Inventory items;
			
			protected override bool IsActionTriggered()
			{
				var possibleDeliveryTargets = Cached
					.Where(m => m.CanDeposit && m.IsRequestingDelivery)
					.OrderBy(m => m.Model.DistanceTo(Agent));

				foreach (var possibleDeliveryTarget in possibleDeliveryTargets)
				{
					if (!GetIsNavigable(possibleDeliveryTarget)) continue;

					var possibleDeliverySources = Cached
						.Where(m => m.CanWithdrawal)
						.OrderBy(m => m.Model.DistanceTo(possibleDeliveryTarget.Model));

					foreach (var possibleDeliverySource in possibleDeliverySources)
					{
						if (!GetIsNavigable(possibleDeliverySource)) continue;

						var isIntersecting = possibleDeliveryTarget.Model.Inventory.Desired.Value.Delivery.Intersects(
							possibleDeliverySource.Model.Inventory.Available.Value,
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
					deliverySource.Model.Inventory,
					deliveryTarget.Model.Inventory
				);
			}
		}
		
		public class ToBalanceOnAvailableDistribution : ToBalanceOnAvailable
		{
			protected override Actions CurrentAction => Actions.Distribution;
			
			Cache distributionSource;
			Cache distributionDestination;
			Inventory items;
			
			protected override bool IsActionTriggered()
			{
				var possibleDistributionSources = Cached
					.Where(m => m.CanWithdrawal && m.IsRequestingDistribution)
					.OrderBy(m => m.Model.DistanceTo(Agent));

				foreach (var possibleDistributionSource in possibleDistributionSources)
				{
					if (!GetIsNavigable(possibleDistributionSource)) continue;

					var possibleDistributionDestinations = Cached
						.Where(m => m.CanDeposit)
						.OrderBy(m => m.Model.DistanceTo(possibleDistributionSource.Model));

					foreach (var possibleDistributionDestination in possibleDistributionDestinations)
					{
						if (!GetIsNavigable(possibleDistributionDestination)) continue;

						var isIntersecting = possibleDistributionSource.Model.Inventory.Desired.Value.Distribution.Intersects(
							possibleDistributionDestination.Model.Inventory.AvailableCapacity.Value.GetCapacityFor(
								possibleDistributionDestination.Model.Inventory.Available.Value	
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
					distributionSource.Model.Inventory,
					distributionDestination.Model.Inventory
				);
			}
		}

		class ToReturnOnFallthrough : AgentTransition<BalanceItemState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => true;
		}		
	}
}