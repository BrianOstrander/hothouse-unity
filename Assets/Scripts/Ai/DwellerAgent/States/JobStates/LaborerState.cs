using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class LaborerState<S> : JobState<S, LaborerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		protected override Jobs Job => Jobs.Laborer;

		public override void OnInitialize()
		{
			base.OnInitialize();
			
			AddChildStates(
				new DestroyGenericHandlerState(),
				new ConstructAssembleHandlerState(),
				new DoorOpenHandlerState(),
				new NavigateState(),
				new InventoryPromiseHandlerState()
				// new BalanceItemState()
			);

			AddTransitions(
				new ToReturnOnJobChanged(),
				new ToReturnOnShiftEnd(),
				
				new InventoryPromiseHandlerState.ToInventoryPromiseHandlerOnAvailable(),

				new DestroyGenericHandlerState.ToObligationOnExistingObligation(),
				new ConstructAssembleHandlerState.ToObligationOnExistingObligation(),
				new DoorOpenHandlerState.ToObligationOnExistingObligation(),
				
				new DestroyGenericHandlerState.ToObligationHandlerOnAvailableObligation(),
				new ConstructAssembleHandlerState.ToObligationHandlerOnAvailableObligation(),
				new DoorOpenHandlerState.ToObligationHandlerOnAvailableObligation(),
				
				// new BalanceItemState.ToBalanceOnAvailableDelivery(),
				// new BalanceItemState.ToBalanceOnAvailableDistribution(),
				
				new NavigateToNearestLight()
			);
		}
		
		#region Child Classes
		class InventoryPromiseHandlerState : InventoryPromiseHandlerState<LaborerState<S>> { } 
		#endregion
	}
}