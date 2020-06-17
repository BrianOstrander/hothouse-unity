using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Seeker
{
	public class IdleState : AgentState<GameModel, SeekerModel>
	{
		public override string Name => "Idle";

		public override void OnInitialize()
		{
			var timeoutState = new TimeoutState<IdleState>();
			
			AddChildStates(
				timeoutState	
			);
			// AddTransitions(
			// 	new DropItemsTransition<IdleState>(timeoutState)
			// );
		}

	}
}