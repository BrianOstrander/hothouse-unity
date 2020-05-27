using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai
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
			return DwellerUtility.CalculateNearestAvailableOperatingEntrance(
				agent.Position.Value,
				out path,
				out entrancePosition,
				b => 0f < b.DesireQualities.Value.FirstAvailableQualityOrDefault(Desire),
				world.Buildings.AllActive
			);
		}
		
		public override string Name => Desire + "Desire";

		public abstract Desires Desire { get; }

		public override void OnInitialize()
		{
			var timeoutState = new DwellerTimeoutState<S>();
			
			AddChildStates(
				timeoutState,
				new DwellerNavigateState<S>()
			);
			
			AddTransitions(
				new ToIdleOnDesireChanged(),
				new ToTimeoutForDesire(timeoutState),
				new ToNavigateToNearestDesireBuilding(),
				new ToIdleOnShiftBegin()
			);
		}

		public class ToDesireOnShiftEnd : AgentTransition<S, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + desireState.Name + ">";

			DwellerDesireState<S> desireState;

			public ToDesireOnShiftEnd(DwellerDesireState<S> desireState) => this.desireState = desireState; 
			
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
		
		protected class ToIdleOnDesireChanged : AgentTransition<DwellerDesireState<S>, DwellerIdleState, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + SourceState.Name + ">";

			public override bool IsTriggered() => SourceState.Desire != Agent.Desire.Value;
		}
		
		protected class ToIdleOnShiftBegin : AgentTransition<DwellerDesireState<S>, DwellerIdleState, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + SourceState.Name + ">";

			public override bool IsTriggered() => Agent.JobShift.Value.Contains(World.SimulationTime.Value);

			public override void Transition()
			{
				if (Agent.GetDesireDamage(SourceState.Desire, out var damage))
				{
					Agent.Health.Value = Mathf.Max(0f, Agent.Health.Value - damage);
				}

				Agent.Desire.Value = Desires.None;
			}
		}
		
		protected class ToTimeoutForDesire : AgentTransition<DwellerDesireState<S>, DwellerTimeoutState<S>, GameModel, DwellerModel>
		{
			DwellerTimeoutState<S> timeoutState;
			BuildingModel target;

			public ToTimeoutForDesire(
				DwellerTimeoutState<S> timeoutState
			)
			{
				this.timeoutState = timeoutState;
			}

			public override bool IsTriggered()
			{
				target = SourceState.GetNearestDesireBuilding(World, Agent, out _, out var entrancePosition);

				if (target == null) return false;

				return Vector3.Distance(Agent.Position.Value.NewY(0f), entrancePosition.NewY(0f)) < Agent.TransferDistance.Value;
			}

			public override void Transition()
			{
				timeoutState.ConfigureForNextTimeOfDay(Agent.JobShift.Value.Begin);
				target.Operate(Agent, SourceState.Desire);
				Agent.Desire.Value = Desires.None;
			}
		}
		
		protected class ToNavigateToNearestDesireBuilding : AgentTransition<DwellerDesireState<S>, DwellerNavigateState<S>, GameModel, DwellerModel>
		{
			BuildingModel target;
			NavMeshPath targetPath = new NavMeshPath();

			public override bool IsTriggered()
			{
				target = SourceState.GetNearestDesireBuilding(World, Agent, out targetPath, out _);

				return target != null;
			}

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(targetPath);
			}
		}
	}
}