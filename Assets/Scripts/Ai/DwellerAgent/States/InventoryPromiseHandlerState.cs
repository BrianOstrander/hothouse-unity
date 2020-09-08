using System;
using Lunra.Hothouse.Models;
using Lunra.Satchel;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class InventoryPromiseHandlerState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		// class Cache
		// {
		// 	public Item PromiseItem;
		// 	public Stack PromiseStack;
		// }
	
		TimeoutState timeoutInstance;
		
		public override void OnInitialize()
		{
			AddChildStates(
				new NavigateState(),
				timeoutInstance = new TimeoutState()
			);
			
			AddTransitions(
				new ToReturnOnNoPromises()	
			);
		}

		public override void Begin()
		{
			while (Agent.InventoryPromises.All.TryPeek(out var promise))
			{
				if (Agent.Inventory.Container.TryFindFirst(promise, out var promiseItem))
				{
					var type = promiseItem[Items.Keys.Shared.Type];
				
					if (type == Items.Values.Shared.Types.Transfer)
					{
						var state = promiseItem[Items.Keys.Transfer.LogisticState];
				
						if (state == Items.Values.Transfer.LogisticStates.Pickup)
						{
							if (OnBeginPickup(promiseItem)) break;
						}
						else if (state == Items.Values.Transfer.LogisticStates.Dropoff)
						{
							if (OnBeginDropoff(promiseItem)) break;
						}
						else Debug.LogError($"Unrecognized {Items.Keys.Transfer.LogisticState}: {state}");
					}
					else Debug.LogError($"Unrecognized {Items.Keys.Shared.Type}: {type}");
				}
				else Debug.LogError($"Cannot find item for promise with id {promise}");

				Agent.InventoryPromises.Break();
			}
		}

		bool OnBeginTryGetReservation(
			Item promiseItem,
			PropertyKey<long> reservationIdKey,
			out Item reservationItem
		)
		{
			var reservationId = promiseItem[reservationIdKey];
			if (Game.Items.TryGet(reservationId, out reservationItem)) return true;

			Debug.LogError($"Unable to find {reservationIdKey} of {reservationId} for item {promiseItem}");
			return false;
		}
		
		bool OnBeginPickup(
			Item promiseItem
		)
		{
			if (!OnBeginTryGetReservation(promiseItem, Items.Keys.Transfer.ReservationPickupId, out var reservationItem)) return false;
			
			Debug.LogError("reserved begin is: "+reservationItem);

			return true;
		}
		
		bool OnBeginDropoff(
			Item promiseItem
		)
		{
			if (!OnBeginTryGetReservation(promiseItem, Items.Keys.Transfer.ReservationDropoffId, out var reservationItem)) return false;

			throw new NotImplementedException();
		}

		public class ToInventoryPromiseHandlerOnAvailable : AgentTransition<S, InventoryPromiseHandlerState<S>, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => Agent.InventoryPromises.All.Any();
		}

		protected class ToReturnOnNoPromises : AgentTransition<InventoryPromiseHandlerState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => Agent.InventoryPromises.All.None();
		}
		
		#region Child Classes
		class NavigateState : NavigateState<InventoryPromiseHandlerState<S>> { }
		class TimeoutState : TimeoutState<InventoryPromiseHandlerState<S>> { }
		#endregion
	}
}