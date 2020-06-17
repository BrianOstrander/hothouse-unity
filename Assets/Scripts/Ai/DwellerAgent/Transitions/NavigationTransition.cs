using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class NavigationTransition<S> : AgentTransition<NavigateState<S>, GameModel, DwellerModel>
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