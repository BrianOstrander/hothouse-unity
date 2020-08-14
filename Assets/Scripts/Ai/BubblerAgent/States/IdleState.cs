using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Bubbler
{
	public class IdleState : AgentState<GameModel, BubblerModel>
	{
		public override string Name => "Idle";

		public override void OnInitialize()
		{
			AddChildStates(
				new NavigateState<IdleState>()
			);
			
			AddTransitions(
				new ToNavigateTest()
			);
		}

		class ToNavigateTest : AgentTransition<IdleState, NavigateState<IdleState>, GameModel, BubblerModel>
		{
			public override bool IsTriggered() => true;

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.NavigatingForced(Agent.Transform.Position.Value, Agent.Transform.Position.Value + (Vector3.forward * 4f));
		}
	}
}