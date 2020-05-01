using System.Linq;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerNavigateState : AgentState<GameModel, DwellerModel>
	{
		public override void OnInitialize()
		{
			AddTransitions(
				new ToIdleOnDone(),
				new ToIdleOnInvalid()
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
			else Agent.NavigationPlan.Value = NavigationPlan.Invalid(Agent.NavigationPlan.Value);

			return hasPath;
		}

		class ToIdleOnDone : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
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
		
		class ToIdleOnInvalid : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
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