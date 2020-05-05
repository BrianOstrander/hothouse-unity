using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerNavigationTransition<S> : AgentTransition<DwellerNavigateState<S>, GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		public override bool IsTriggered()
		{
			switch (Agent.NavigationPlan.Value.State)
			{
				case NavigationPlan.States.Calculating:
				case NavigationPlan.States.Navigating:
					return true;
			}

			return false;
		}
	}
}