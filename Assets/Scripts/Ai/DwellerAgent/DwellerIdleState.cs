using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerIdleState : AgentState<GameModel, DwellerModel>
	{
		public override void OnInitialize()
		{
			AddTransitions(
				new ToNavigate()	
			);
		}

		class ToNavigate : AgentTransition<DwellerNavigateState, GameModel, DwellerModel>
		{
			public override bool IsTriggered()
			{
				return Agent.NavigationPlan.Value.State == NavigationPlan.States.Calculating;
			}
		}
	}
}