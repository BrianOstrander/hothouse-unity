using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;

namespace Lunra.Hothouse.Ai
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