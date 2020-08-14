using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Bubbler
{
	public class IdleState : AgentState<GameModel, BubblerModel>
	{
		public override string Name => "Idle";

		public override void OnInitialize()
		{
			
		}
	}
}