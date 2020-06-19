using Lunra.Hothouse.Models;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class NavigationForcedTransition<S> : AgentTransition<NavigateState<S>, GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		Vector3 nearestValidPosition;
			
		public override bool IsTriggered()
		{
			if (Agent.NavigationPlan.Value.State != NavigationPlan.States.Invalid) return false;

			if (NavMesh.SamplePosition(Agent.Transform.Position.Value, out var hit, Agent.NavigationForceDistanceMaximum.Value, NavMesh.AllAreas))
			{
				nearestValidPosition = hit.position;
				return !Mathf.Approximately(0f, Vector3.Distance(Agent.Transform.Position.Value, hit.position));
			}

			return false;
		}

		public override void Transition()
		{
			Agent.NavigationPlan.Value = NavigationPlan.NavigatingForced(
				Agent.Transform.Position.Value,
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