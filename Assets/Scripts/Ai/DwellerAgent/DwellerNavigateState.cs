using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using UnityEngine.AI;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerNavigateState : AgentState<GameModel, DwellerModel>
	{
		public override void OnInitialize()
		{
			AddTransitions(
				new ToIdleOnDoneOrInvalid()
			);
		}

		public override void Begin() => CalculatePath();

		public override void Idle(float delta)
		{
			if (Agent.NavigationPlan.Value.Created < World.LastNavigationCalculation.Value)
			{
				if (!CalculatePath()) return;
			}
			
			Agent.NavigationPlan.Value = Agent.NavigationPlan.Value.Next(Agent.NavigationVelocity.Value * delta);
		}

		bool CalculatePath()
		{
			var path = new NavMeshPath();
			var hasPath = NavMesh.CalculatePath(
				Agent.NavigationPlan.Value.Position,
				Agent.NavigationPlan.Value.EndPosition,
				NavMesh.AllAreas,
				path
			);

			if (hasPath) Agent.NavigationPlan.Value = NavigationPlan.Navigating(path);
			else Agent.NavigationPlan.Value = NavigationPlan.Invalid();

			return hasPath;
		}

		class ToIdleOnDoneOrInvalid : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
		{
			public override bool IsTriggered()
			{
				switch (Agent.NavigationPlan.Value.State)
				{
					case NavigationPlan.States.Done:
					case NavigationPlan.States.Invalid:
						return true;
				}
				return false;
			}
		}
	}
}