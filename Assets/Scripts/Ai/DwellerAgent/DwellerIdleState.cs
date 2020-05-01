using System.Linq;
using Lunra.Core;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerIdleState : AgentState<GameModel, DwellerModel>
	{
		public override void OnInitialize()
		{
			AddTransitions(
				new ToNavigateForced(),
				new ToNavigate(),
				new ToClearNearestFlora(),
				new ToNavigateToNearestFlora()
			);
		}

		class ToNavigateForced : AgentTransition<DwellerNavigateState, GameModel, DwellerModel>
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
		
		class ToNavigate : AgentTransition<DwellerNavigateState, GameModel, DwellerModel>
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
		
		class ToNavigateToNearestFlora : AgentTransition<DwellerNavigateState, GameModel, DwellerModel>
		{
			FloraModel targetFlora;
			
			public override bool IsTriggered()
			{
				if (Agent.Job.Value != DwellerModel.Jobs.ClearFlora) return false;

				targetFlora = World.Flora.GetActive()
					.Where(
						flora =>
						{
							if (!flora.MarkedForClearing.Value) return false;
							if (flora.NavigationPoint.Value.Access != NavigationProximity.AccessStates.Accessible) return false;
							return World.Dwellers.GetActive().None(
								d =>
								{
									if (d == Agent) return false;
									var distanceBetweenCurrent = Vector3.Distance(flora.NavigationPoint.Value.Position, d.Position.Value);
									var distanceBetweenTarget = Vector3.Distance(flora.NavigationPoint.Value.Position, d.NavigationPlan.Value.EndPosition);
									return Mathf.Min(distanceBetweenCurrent, distanceBetweenTarget) < Agent.MeleeRange.Value;
								}
							);
						}
					)
					.OrderBy(f => Vector3.Distance(Agent.Position.Value, f.NavigationPoint.Value.Position))
					.FirstOrDefault();
					// .RandomWeighted(f => Vector3.Distance(Agent.Position.Value, f.NavigationPoint.Value.Position));

				return targetFlora != null;
			}

			public override void Transition()
			{
				if (targetFlora != null) Agent.NavigationPlan.Value = NavigationPlan.Calculating(Agent.Position.Value, targetFlora.NavigationPoint.Value.Position);
				else
				{
					Debug.LogError(nameof(targetFlora) + " is null, this should not occur");
					Agent.NavigationPlan.Value = NavigationPlan.Invalid(Agent.Position.Value);
				}
			}
		}
		
		class ToClearNearestFlora : AgentTransition<DwellerClearFloraState, GameModel, DwellerModel>
		{
			public override bool IsTriggered()
			{
				if (Agent.Job.Value != DwellerModel.Jobs.ClearFlora) return false;

				var validFlora = World.Flora.GetActive().FirstOrDefault(
					flora =>
					{
						if (flora.State.Value == FloraModel.States.Pooled) return false;
						if (!flora.MarkedForClearing.Value) return false;
						if (flora.NavigationPoint.Value.Access != NavigationProximity.AccessStates.Accessible) return false;
						return Vector3.Distance(Agent.Position.Value, flora.Position.Value) < Agent.MeleeRange.Value;
					}
				);

				return validFlora != null;
			}
		}
	}
}