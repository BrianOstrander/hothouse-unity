using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Bubbler
{
	public class IdleState : AgentState<GameModel, BubblerModel>
	{
		public override string Name => "Idle";

		TimeoutState timeoutState;
		
		public override void OnInitialize()
		{
			AddChildStates(
				timeoutState = new TimeoutState(),
				new NavigateState(),
				new WanderState()
			);
			
			AddTransitions(
				new WanderState.ToWander(),
				new ToTimeoutOnFallthrough()
			);
		}

		class ToTimeoutOnFallthrough : AgentTransition<IdleState, TimeoutState, GameModel, BubblerModel>
		{
			public override bool IsTriggered() => true;

			public override void Transition() => SourceState.timeoutState.ConfigureForNextTimeOfDay(0.25f);
		}
		
		protected class NavigateState : BaseNavigateState<IdleState, BubblerModel> { }
		protected class TimeoutState : BaseTimeoutState<IdleState, BubblerModel> { }
		protected class WanderState : WanderState<IdleState> { }
	}
}