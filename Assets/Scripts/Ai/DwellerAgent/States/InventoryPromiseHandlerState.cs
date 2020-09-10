using System;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Satchel;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class InventoryPromiseHandlerState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		class Cache
		{
			public bool IsValid;
			public bool IsPickup;
			public bool IsAgentCarryingItem;
			public bool IsInteractionInRange;
			public Item Item;
			public Item TransferItem;
			public Item ReservationItem;
			public IInventoryModel ReservationInventory;
			public Navigation.Result NavigationResult;
		}
	
		TimeoutState timeoutInstance;
		Cache cache;
		
		public override void OnInitialize()
		{
			AddChildStates(
				new NavigateState(),
				timeoutInstance = new TimeoutState()
			);
			
			AddTransitions(
				new ToReturnOnNoPromises(),
				new ToTimeoutOnPickup(),
				new ToTimeoutOnDropoff(),
				new ToNavigateToPickup(),
				new ToNavigateToDropoff()
			);
		}

		public override void Begin()
		{
			cache = new Cache();
			
			while (Agent.InventoryPromises.All.TryPeek(out var promise))
			{
				if (Agent.Inventory.Container.TryFindFirst(promise, out cache.TransferItem))
				{
					var type = cache.TransferItem[Items.Keys.Shared.Type];
				
					if (type == Items.Values.Shared.Types.Transfer)
					{
						var state = cache.TransferItem[Items.Keys.Transfer.LogisticState];
				
						if (state == Items.Values.Transfer.LogisticStates.Pickup)
						{
							cache.IsValid = OnBeginCalculateCache(Items.Keys.Transfer.ReservationPickupId);
							cache.IsPickup = true;
						}
						else if (state == Items.Values.Transfer.LogisticStates.Dropoff)
						{
							cache.IsValid = OnBeginCalculateCache(Items.Keys.Transfer.ReservationDropoffId);
						}
						else Debug.LogError($"Unrecognized {Items.Keys.Transfer.LogisticState}: {state}");
					}
					else Debug.LogError($"Unrecognized {Items.Keys.Shared.Type}: {type}");
				}
				else Debug.LogError($"Cannot find item for promise with id {promise}");

				if (cache.IsValid) break;
				
				Agent.InventoryPromises.Break();
			}
		}

		bool OnBeginCalculateCache(PropertyKey<long> reservationIdKey)
		{
			var itemId = cache.TransferItem[Items.Keys.Transfer.ItemId];

			if (!Game.Items.TryGet(itemId, out cache.Item))
			{
				Debug.LogError($"Unable to find item {itemId} of transfer {cache.TransferItem}");
				return false;
			}

			cache.IsAgentCarryingItem = cache.Item.ContainerId == Agent.Inventory.Container.Id;
			
			var reservationItemId = cache.TransferItem[reservationIdKey];
			if (!Game.Items.TryGet(reservationItemId, out cache.ReservationItem))
			{
				Debug.LogError($"Unable to find {reservationIdKey} for reservation {reservationItemId} of transfer {cache.TransferItem}");
				return false;
			}

			var reservationContainerId = cache.ReservationItem.ContainerId;

			if (!Game.Query.TryFindFirst(m => m.Inventory.Container.Id == reservationContainerId, out cache.ReservationInventory))
			{
				Debug.LogError($"Unable to find an inventory with container id {reservationContainerId} for reservation {cache.ReservationItem} of transfer {cache.TransferItem}");
				return false;
			}

			if (!Navigation.TryQuery(cache.ReservationInventory, out var navigationQuery))
			{
				Debug.LogError($"Unable to create a navigation query for {cache.ReservationInventory}");
				return false;
			}
			
			var isNavigable = NavigationUtility.CalculateNearest(
				Agent.Transform.Position.Value,
				out cache.NavigationResult,
				navigationQuery
			);

			cache.IsInteractionInRange = Vector3.Distance(Agent.Transform.Position.Value, cache.NavigationResult.Target) <= Agent.InteractionRadius.Value; 

			return isNavigable;
		}
		
		public class ToInventoryPromiseHandlerOnAvailable : AgentTransition<S, InventoryPromiseHandlerState<S>, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => Agent.InventoryPromises.All.Any();
		}

		class ToReturnOnNoPromises : AgentTransition<InventoryPromiseHandlerState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => Agent.InventoryPromises.All.None();
		}

		class ToTimeoutOnPickup : AgentTransition<InventoryPromiseHandlerState<S>, TimeoutState, GameModel, DwellerModel>
		{
			public override bool IsTriggered()
			{
				if (!SourceState.cache.IsPickup) return false;
				if (SourceState.cache.IsAgentCarryingItem) return false;
				return SourceState.cache.IsInteractionInRange;
			}

			public override void Transition()
			{
				SourceState.cache.ReservationInventory.Inventory.Container
					.Destroy(SourceState.cache.ReservationItem);

				Container.Transfer(
					SourceState.cache.Item.StackOfAll(),
					SourceState.cache.ReservationInventory.Inventory.Container,
					Agent.Inventory.Container
				);
				
				SourceState.cache.Item[Items.Keys.Resource.LogisticState] = Items.Values.Resource.LogisticStates.None;
				
				SourceState.cache.TransferItem.Set(
					(Items.Keys.Transfer.ReservationPickupId, IdCounter.UndefinedId),
					(Items.Keys.Transfer.LogisticState, Items.Values.Transfer.LogisticStates.Dropoff)
				);
				
				SourceState.timeoutInstance.ConfigureForInterval(DayTime.FromRealSeconds(0.5f));
			}
		}
		
		class ToTimeoutOnDropoff : AgentTransition<InventoryPromiseHandlerState<S>, TimeoutState, GameModel, DwellerModel>
		{
			public override bool IsTriggered()
			{
				if (SourceState.cache.IsPickup) return false;
				if (!SourceState.cache.IsAgentCarryingItem) return false;
				return SourceState.cache.IsInteractionInRange;
			}

			public override void Transition()
			{
				SourceState.cache.ReservationInventory.Inventory.Container
					.Destroy(SourceState.cache.ReservationItem);

				Container.Transfer(
					SourceState.cache.Item.StackOfAll(),
					Agent.Inventory.Container,
					SourceState.cache.ReservationInventory.Inventory.Container
				);
				
				Agent.Inventory.Container
					.Destroy(SourceState.cache.TransferItem);

				Agent.InventoryPromises.All.Pop();
				
				SourceState.timeoutInstance.ConfigureForInterval(DayTime.FromRealSeconds(0.5f));
			}
		}
		
		class ToNavigateToPickup : AgentTransition<InventoryPromiseHandlerState<S>, NavigateState, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => SourceState.cache.IsPickup && !SourceState.cache.IsAgentCarryingItem;

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(SourceState.cache.NavigationResult.Path);
			}
		}
		
		class ToNavigateToDropoff : AgentTransition<InventoryPromiseHandlerState<S>, NavigateState, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => !SourceState.cache.IsPickup && SourceState.cache.IsAgentCarryingItem;

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(SourceState.cache.NavigationResult.Path);
			}
		}
		
		#region Child Classes
		class NavigateState : NavigateState<InventoryPromiseHandlerState<S>> { }
		class TimeoutState : TimeoutState<InventoryPromiseHandlerState<S>> { }
		#endregion
	}
}