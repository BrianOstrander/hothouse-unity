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
				new DestroyMeleeHandlerState(),
				new ConstructAssembleHandlerState(),
				new NavigateState()
			);
			
			AddTransitions(
				new ToReturnOnJobChanged(),
				new ToReturnOnShiftEnd(),
				
				new DestroyMeleeHandlerState.ToObligationOnExistingObligation(),
				new ConstructAssembleHandlerState.ToObligationOnExistingObligation(),
				
				new DestroyMeleeHandlerState.ToObligationHandlerOnAvailableObligation(),
				new ConstructAssembleHandlerState.ToObligationHandlerOnAvailableObligation(),
				
				new NavigateToNearestLight()
			);
		}
	}
}