using System.Linq;
using Lunra.Core;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using UnityEngine;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerIdleState : AgentState<GameModel, DwellerModel>
	{
		public override void OnInitialize()
		{
			AddTransitions(
				new ToNavigate(),
				new ToNavigateToNearestFlora(),
				new ToClearNearestFlora()
			);
		}

		class ToNavigate : AgentTransition<DwellerNavigateState, GameModel, DwellerModel>
		{
			public override bool IsTriggered()
			{
				return Agent.NavigationPlan.Value.State == NavigationPlan.States.Calculating;
			}
		}
		
		class ToNavigateToNearestFlora : AgentTransition<DwellerNavigateState, GameModel, DwellerModel>
		{
			public override bool IsTriggered()
			{
				if (Agent.Job.Value != DwellerModel.Jobs.ClearFlora) return false;

				var anyValidFlora = false;
				var validFlora = World.Flora.GetActive().FirstOrDefault(
					flora =>
					{
						if (flora.NavigationPoint.Value.Access != NavigationProximity.AccessStates.Accessible) return false;
						anyValidFlora = true;
						return Mathf.Approximately(0f, Vector3.Distance(Agent.Position.Value, flora.NavigationPoint.Value.Position));
					}
				);

				return validFlora == null && anyValidFlora;
			}

			public override void Transition()
			{
				var targetFlora = World.Flora.GetActive()
					.Where(f => f.NavigationPoint.Value.Access == NavigationProximity.AccessStates.Accessible)
					.RandomWeighted(f => Vector3.Distance(Agent.Position.Value, f.NavigationPoint.Value.Position));
					// .OrderBy(f => Vector3.Distance(Agent.Position.Value, f.NavigationPoint.Value.Position))
					// .FirstOrDefault();
				
				if (targetFlora != null) Agent.NavigationPlan.Value = NavigationPlan.Calculating(Agent.Position.Value, targetFlora.NavigationPoint.Value.Position);
				else
				{
					Debug.LogError("No valid flora available, this should not occur");
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
						if (flora.NavigationPoint.Value.Access != NavigationProximity.AccessStates.Accessible) return false;
						return Mathf.Approximately(0f, Vector3.Distance(Agent.Position.Value, flora.NavigationPoint.Value.Position));
					}
				);

				return validFlora != null;
			}
		}
	}
}