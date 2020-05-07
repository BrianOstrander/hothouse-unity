using Lunra.Core;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.WildVacuum.Ai
{
	public abstract class DwellerDesireState<S> : AgentState<GameModel, DwellerModel>
		where S : DwellerDesireState<S>
	{
		protected BuildingModel GetNearestDesireBuilding(
			GameModel world,
			DwellerModel agent,
			out NavMeshPath path,
			out Vector3 entrancePosition
		)
		{
			return DwellerUtility.CalculateNearestEntrance(
				agent.Position.Value,
				world.Buildings.AllActive,
				b =>
				{
					if (b.DesireQuality.Value.TryGetValue(Desire, out var quality)) return 0f < quality;
					return false;
				},
				out path,
				out entrancePosition
			);
		}
		
		public override string Name => Desire + "Desire";

		public abstract Desires Desire { get; }

		ToDesireOnShiftEnd toDesireOnShiftEnd;
		public ToDesireOnShiftEnd GetToDesireOnShiftEnd => toDesireOnShiftEnd ?? (toDesireOnShiftEnd = new ToDesireOnShiftEnd(this as S));
		
		public override void OnInitialize()
		{
			var timeoutState = new DwellerTimeoutState<S>();
			
			AddChildStates(
				timeoutState,
				new DwellerNavigateState<DwellerSleepDesireState>()
			);
			
			AddTransitions(
				new ToIdleOnDesireChanged(this as S),
				new ToTimeoutForDesire(this as S, timeoutState),
				new ToNavigateToNearestDesireBuilding(this as S),
				new ToIdleOnShiftBegin(this as S)
			);
		}

		public class ToDesireOnShiftEnd : AgentTransition<S, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + desireState.Name + ">";

			S desireState;

			public ToDesireOnShiftEnd(S desireState) => this.desireState = desireState; 
			
			public override bool IsTriggered()
			{
				switch (Agent.Desire.Value)
				{
					case Desires.Unknown:
					case Desires.None:
						return false;
				}
				
				return Agent.Desire.Value == desireState.Desire && !Agent.JobShift.Value.Contains(World.SimulationTime.Value);
			}
		}
		
		protected class ToIdleOnDesireChanged : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + desireState.Name + ">";
			
			S desireState;

			public ToIdleOnDesireChanged(S desireState) => this.desireState = desireState; 
			
			public override bool IsTriggered() => desireState.Desire != Agent.Desire.Value;
		}
		
		protected class ToIdleOnShiftBegin : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + desireState.Name + ">";
			
			S desireState;

			public ToIdleOnShiftBegin(S desireState) => this.desireState = desireState; 
			
			public override bool IsTriggered() => Agent.JobShift.Value.Contains(World.SimulationTime.Value);
		}
		
		protected class ToTimeoutForDesire : AgentTransition<DwellerTimeoutState<S>, GameModel, DwellerModel>
		{
			S desireState;
			DwellerTimeoutState<S> timeoutState;
			BuildingModel target;

			public ToTimeoutForDesire(
				S desireState,
				DwellerTimeoutState<S> timeoutState
			)
			{
				this.desireState = desireState;
				this.timeoutState = timeoutState;
			}

			public override bool IsTriggered()
			{
				target = desireState.GetNearestDesireBuilding(World, Agent, out _, out var entrancePosition);

				if (target == null) return false;

				return Mathf.Approximately(0f, Vector3.Distance(Agent.Position.Value.NewY(0f), entrancePosition.NewY(0f)));
			}

			public override void Transition()
			{
				timeoutState.ConfigureForNextTimeOfDay(Agent.JobShift.Value.Begin);
				Agent.Desire.Value = Desires.None;
			}
		}
		
		protected class ToNavigateToNearestDesireBuilding : AgentTransition<DwellerNavigateState<DwellerSleepDesireState>, GameModel, DwellerModel>
		{
			S desireState;
			BuildingModel target;
			NavMeshPath targetPath = new NavMeshPath();

			public ToNavigateToNearestDesireBuilding(S desireState) => this.desireState = desireState;
			
			public override bool IsTriggered()
			{
				target = desireState.GetNearestDesireBuilding(World, Agent, out targetPath, out _);

				return target != null;
			}

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(targetPath);
			}
		}
	}
}