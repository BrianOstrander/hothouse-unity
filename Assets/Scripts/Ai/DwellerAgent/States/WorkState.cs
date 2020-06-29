using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class WorkState : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "Work";

		public override void OnInitialize()
		{
			var timeoutState = new TimeoutState<WorkState>();
			
			AddChildStates(
				timeoutState	
			);
			
			AddTransitions(
				
			);
		}
		
		
	}
}