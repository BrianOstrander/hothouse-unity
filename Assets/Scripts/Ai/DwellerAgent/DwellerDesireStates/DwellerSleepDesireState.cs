using System.Linq;
using Lunra.Core;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerSleepDesireState : DwellerDesireState<DwellerSleepDesireState>
	{
		static DesireBuildingModel GetNearestSleepBuilding(
			GameModel world,
			DwellerModel agent,
			out NavMeshPath path,
			out Vector3 entrancePosition
		)
		{
			return DwellerUtility.CalculateNearestEntrance(
				agent.Position.Value,
				world.DesireBuildings.AllActive,
				b =>
				{
					if (b.DesireQuality.Value.TryGetValue(Desires.Sleep, out var quality)) return 0f < quality;
					return false;
				},
				out path,
				out entrancePosition
			);
		}
		
		public override Desires Desire => Desires.Sleep;
		
		public override void OnInitialize()
		{
			var timeoutState = new DwellerTimeoutState<DwellerSleepDesireState>();
			
			AddChildStates(
				timeoutState,
				new DwellerNavigateState<DwellerSleepDesireState>()
			);
			
			AddTransitions(
				new ToIdleOnDesireChanged(this),
				new ToTimeoutForSleep(timeoutState),
				new ToNavigateToNearestSleepBuilding(),
				new ToIdleOnShiftBegin(this)
			);
		}
		
		class ToTimeoutForSleep : AgentTransition<DwellerTimeoutState<DwellerSleepDesireState>, GameModel, DwellerModel>
		{
			DwellerTimeoutState<DwellerSleepDesireState> timeoutState;
			BuildingModel target;

			public ToTimeoutForSleep(DwellerTimeoutState<DwellerSleepDesireState> timeoutState) => this.timeoutState = timeoutState;
			
			public override bool IsTriggered()
			{
				target = GetNearestSleepBuilding(World, Agent, out _, out var entrancePosition);

				if (target == null) return false;

				return Mathf.Approximately(0f, Vector3.Distance(Agent.Position.Value.NewY(0f), entrancePosition.NewY(0f)));
			}

			public override void Transition()
			{
				timeoutState.ConfigureForNextTimeOfDay(Agent.JobShift.Value.Begin);
				Agent.Desire.Value = Desires.None;
			}
		}
		
		class ToNavigateToNearestSleepBuilding : AgentTransition<DwellerNavigateState<DwellerSleepDesireState>, GameModel, DwellerModel>
		{
			DesireBuildingModel target;
			NavMeshPath targetPath = new NavMeshPath();
			
			public override bool IsTriggered()
			{
				target = GetNearestSleepBuilding(World, Agent, out targetPath, out _);

				return target != null;
			}

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(targetPath);
			}
		}
	}
}