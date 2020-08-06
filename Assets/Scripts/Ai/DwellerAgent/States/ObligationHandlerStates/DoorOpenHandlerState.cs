using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class DoorOpenHandlerState<S> : ObligationHandlerState<S, DoorOpenHandlerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		static readonly Obligation[] DefaultObligationsHandled =
		{
			ObligationCategories.Door.Open
		};

		public override Obligation[] ObligationsHandled => DefaultObligationsHandled;

		public override void OnInitialize()
		{
			AddChildStates(
				new NavigateState(),
				TimeoutInstance = new TimeoutState()
			);
			
			AddTransitions(
				new ToReturnOnMissingObligation(),
				new ToReturnOnTimeout(),
				
				new ToTimeoutOnOpenTarget(),
				
				new ToNavigateToTarget()
			);
		}

		class ToTimeoutOnOpenTarget : ToTimeoutOnTarget
		{
			protected override bool CanPopObligation => true;

			protected override DayTime TimeoutDuration => DayTime.FromMinutes(1f);
		}
	}
}