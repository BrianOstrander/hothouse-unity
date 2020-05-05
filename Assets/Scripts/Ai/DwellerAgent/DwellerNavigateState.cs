using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerNavigateState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "Navigate";

		public override void OnInitialize()
		{
			AddTransitions(
				new ToReturnOnDone(),
				new ToReturnOnInvalid()
			);
		}

		public override void Begin()
		{
			switch (Agent.NavigationPlan.Value.State)
			{
				case NavigationPlan.States.Navigating:
				case NavigationPlan.States.NavigatingForced:
					break;
				default:
					CalculatePath();
					break;
			}
		}

		public override void Idle()
		{
			if (Agent.NavigationPlan.Value.Created < World.LastNavigationCalculation.Value && Agent.NavigationPlan.Value.State != NavigationPlan.States.NavigatingForced)
			{
				if (!CalculatePath()) return;
			}
			
			Agent.NavigationPlan.Value = Agent.NavigationPlan.Value.Next(Agent.NavigationVelocity.Value * World.SimulationDelta);
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
			else Agent.NavigationPlan.Value = NavigationPlan.Invalid(Agent.NavigationPlan.Value);

			return hasPath;
		}

		class ToReturnOnDone : AgentTransition<S, GameModel, DwellerModel>
		{
			public override bool IsTriggered()
			{
				switch (Agent.NavigationPlan.Value.State)
				{
					case NavigationPlan.States.Done:
						return true;
				}
				return false;
			}
		}
		
		class ToReturnOnInvalid : AgentTransition<S, GameModel, DwellerModel>
		{
			public override bool IsTriggered()
			{
				switch (Agent.NavigationPlan.Value.State)
				{
					case NavigationPlan.States.Invalid:
						return true;
				}
				return false;
			}
		}
	}
}