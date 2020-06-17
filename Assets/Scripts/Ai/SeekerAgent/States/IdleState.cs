using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Seeker
{
	public class IdleState : AgentState<GameModel, SeekerModel>
	{
		public override string Name => "Idle";

		public override void OnInitialize()
		{
			AddChildStates(
				new TimeoutState<IdleState>(),
				new HuntState()
			);
			
			AddTransitions(
				new AgentTransitionFallthrough<HuntState, GameModel, SeekerModel>("Hunt")
			);
		}

	}
}