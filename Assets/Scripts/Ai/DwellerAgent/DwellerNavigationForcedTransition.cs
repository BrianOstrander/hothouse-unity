using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai
{
	public class DwellerNavigationForcedTransition<S> : AgentTransition<DwellerNavigateState<S>, GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		Vector3 nearestValidPosition;
			
		public override bool IsTriggered()
		{
			if (Agent.NavigationPlan.Value.State != NavigationPlan.States.Invalid) return false;

			if (NavMesh.SamplePosition(Agent.Position.Value, out var hit, Agent.NavigationForceDistanceMaximum.Value, NavMesh.AllAreas))
			{
				nearestValidPosition = hit.position;
				return !Mathf.Approximately(0f, Vector3.Distance(Agent.Position.Value, hit.position));
			}

			return false;
		}

		public override void Transition()
		{
			Agent.NavigationPlan.Value = NavigationPlan.NavigatingForced(
				Agent.Position.Value,
				nearestValidPosition
			);

			if (Agent.IsDebugging)
			{
				Debug.DrawLine(
					Agent.NavigationPlan.Value.BeginPosition,
					Agent.NavigationPlan.Value.EndPosition,
					Color.magenta,
					30f
				);
			}
		}	
	}
}