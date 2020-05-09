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
			return DwellerUtility.CalculateNearestEntrance(
				agent.Position.Value,
				world.Buildings.AllActive,
				b => 0f < b.DesireQuality.Value.FirstAvailableQualityOrDefault(Desire),
				out path,
				out entrancePosition
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
				new ToIdleOnDesireChanged(this),
				new ToTimeoutForDesire(this, timeoutState),
				new ToNavigateToNearestDesireBuilding(this),
				new ToIdleOnShiftBegin(this)
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
		
		protected class ToIdleOnDesireChanged : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + desireState.Name + ">";
			
			DwellerDesireState<S> desireState;

			public ToIdleOnDesireChanged(DwellerDesireState<S> desireState) => this.desireState = desireState; 
			
			public override bool IsTriggered() => desireState.Desire != Agent.Desire.Value;
		}
		
		protected class ToIdleOnShiftBegin : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + desireState.Name + ">";
			
			DwellerDesireState<S> desireState;

			public ToIdleOnShiftBegin(DwellerDesireState<S> desireState) => this.desireState = desireState; 
			
			public override bool IsTriggered() => Agent.JobShift.Value.Contains(World.SimulationTime.Value);

			public override void Transition()
			{
				if (Agent.GetDesireDamage(desireState.Desire, out var damage))
				{
					Agent.Health.Value = Mathf.Max(0f, Agent.Health.Value - damage);
				}

				Agent.Desire.Value = Desires.None;
			}
		}
		
		protected class ToTimeoutForDesire : AgentTransition<DwellerTimeoutState<S>, GameModel, DwellerModel>
		{
			DwellerDesireState<S> desireState;
			DwellerTimeoutState<S> timeoutState;
			BuildingModel target;

			public ToTimeoutForDesire(
				DwellerDesireState<S> desireState,
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
				target.Operate(Agent, desireState.Desire);
				Agent.Desire.Value = Desires.None;
			}
		}
		
		protected class ToNavigateToNearestDesireBuilding : AgentTransition<DwellerNavigateState<S>, GameModel, DwellerModel>
		{
			DwellerDesireState<S> desireState;
			BuildingModel target;
			NavMeshPath targetPath = new NavMeshPath();

			public ToNavigateToNearestDesireBuilding(DwellerDesireState<S> desireState) => this.desireState = desireState;
			
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