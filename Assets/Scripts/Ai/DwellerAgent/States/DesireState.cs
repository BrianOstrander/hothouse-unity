using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai.Dweller
{
	public abstract class DesireState<S> : AgentState<GameModel, DwellerModel>
		where S : DesireState<S>
	{
		protected BuildingModel GetNearestDesireBuilding(
			GameModel world,
			DwellerModel agent,
			out NavMeshPath path,
			out Vector3 entrancePosition
		)
		{
			return DwellerUtility.CalculateNearestAvailableOperatingEntrance(
				agent.Transform.Position.Value,
				out path,
				out entrancePosition,
				b => 0f < b.DesireQualities.Value.FirstAvailableQualityOrDefault(Desire),
				world.Buildings.AllActive
			);
		}
		
		public override string Name => Desire + "Desire";

		public abstract Desires Desire { get; }

		TimeoutState<S> timeoutState;
		TimeSpan lastEmotedMissedDesire = TimeSpan.FromSeconds(-9999.0);

		public override void OnInitialize()
		{
			
			AddChildStates(
				timeoutState = new TimeoutState<S>(),
				new NavigateState<S>()
			);
			
			AddTransitions(
				new ToIdleOnDesireChanged(),
				new ToTimeoutForDesire(),
				new ToNavigateToNearestDesireBuilding(),
				new ToNavigateToNearestLightSourceOnDesireMissed(),
				new ToTimeoutForDesireMissed(),
				
				new ToIdleOnShiftBegin()
			);
		}

		bool CanEmoteMissedDesire => Agent.DesireMissedEmoteTimeout.Value < (Game.SimulationPlaytimeElapsed.Value - lastEmotedMissedDesire).Seconds;

		IEnterableModel CalculateNearestAvailableEntranceToEmoteMissedDesire(
			out NavMeshPath path,
			out Vector3 entrancePosition
		)
		{
			return DwellerUtility.CalculateNearestAvailableEntrance(
				Agent.Transform.Position.Value,
				out path,
				out entrancePosition,
				Game.Buildings.AllActive.Where(m => m.IsBuildingState(BuildingStates.Operating) && m.Light.IsLightActive()).ToArray()
			);
		}
		
		public class ToDesireOnShiftEnd : AgentTransition<S, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + desireState.Name + ">";

			DesireState<S> desireState;

			public ToDesireOnShiftEnd(DesireState<S> desireState) => this.desireState = desireState; 
			
			public override bool IsTriggered()
			{
				switch (Agent.Desire.Value)
				{
					case Desires.Unknown:
					case Desires.None:
						return false;
				}
				
				return Agent.Desire.Value == desireState.Desire && !Agent.JobShift.Value.Contains(Game.SimulationTime.Value);
			}
		}
		
		protected class ToIdleOnDesireChanged : AgentTransition<DesireState<S>, IdleState, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + SourceState.Name + ">";

			public override bool IsTriggered() => SourceState.Desire != Agent.Desire.Value;
		}
		
		protected class ToIdleOnShiftBegin : AgentTransition<DesireState<S>, IdleState, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + SourceState.Name + ">";

			public override bool IsTriggered() => Agent.JobShift.Value.Contains(Game.SimulationTime.Value);

			public override void Transition()
			{
				if (Agent.GetDesireDamage(SourceState.Desire, Game, out var damage))
				{
					if (Damage.GetDamageTypeFromDesire(SourceState.Desire, out var damageType))
					{
						Damage.Apply(
							damageType,
							damage,
							Agent
						);
					}
					else Debug.Log("Unable to find a damage type for desire: "+SourceState.Desire);
				}
				
				Agent.Desire.Value = Desires.None;
			}
		}
		
		protected class ToTimeoutForDesire : AgentTransition<DesireState<S>, TimeoutState<S>, GameModel, DwellerModel>
		{
			BuildingModel target;

			public override bool IsTriggered()
			{
				target = SourceState.GetNearestDesireBuilding(Game, Agent, out _, out var entrancePosition);

				if (target == null) return false;

				return Vector3.Distance(Agent.Transform.Position.Value.NewY(0f), entrancePosition.NewY(0f)) < Agent.TransferDistance.Value;
			}

			public override void Transition()
			{
				SourceState.timeoutState.ConfigureForNextTimeOfDay(Agent.JobShift.Value.Begin);
				target.Operate(Agent, SourceState.Desire);
				Agent.DesireUpdated(Agent.Desire.Value, true);
				Agent.Desire.Value = Desires.None;
			}
		}
		
		protected class ToNavigateToNearestDesireBuilding : AgentTransition<DesireState<S>, NavigateState<S>, GameModel, DwellerModel>
		{
			BuildingModel target;
			NavMeshPath targetPath = new NavMeshPath();

			public override bool IsTriggered()
			{
				target = SourceState.GetNearestDesireBuilding(Game, Agent, out targetPath, out _);

				return target != null;
			}

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(targetPath);
			}
		}
		
		protected class ToTimeoutForDesireMissed : AgentTransition<DesireState<S>, TimeoutState<S>, GameModel, DwellerModel>
		{
			public override bool IsTriggered()
			{
				if (!SourceState.CanEmoteMissedDesire) return false;
				
				var target = SourceState.CalculateNearestAvailableEntranceToEmoteMissedDesire(
					out _,
					out var entrancePosition
				);

				return target == null || Vector3.Distance(Agent.Transform.Position.Value.NewY(0f), entrancePosition.NewY(0f)) < Agent.ObligationDistance.Value;
			}

			public override void Transition()
			{
				SourceState.timeoutState.ConfigureForNextTimeOfDay(
					Agent.JobShift.Value.Begin,
					delta =>
					{
						if (delta.IsDone) SourceState.lastEmotedMissedDesire = Game.SimulationPlaytimeElapsed.Value; 	
					}
				);
				Agent.DesireUpdated(Agent.Desire.Value, false);
			}
		}
		
		protected class ToNavigateToNearestLightSourceOnDesireMissed : AgentTransition<DesireState<S>, NavigateState<S>, GameModel, DwellerModel>
		{
			NavMeshPath targetPath = new NavMeshPath();

			public override bool IsTriggered()
			{
				if (!SourceState.CanEmoteMissedDesire) return false;

				var target = SourceState.CalculateNearestAvailableEntranceToEmoteMissedDesire(
					out targetPath,
					out var entrancePosition
				);

				if (target == null) return false;
				
				return Agent.ObligationDistance.Value < Vector3.Distance(Agent.Transform.Position.Value.NewY(0f), entrancePosition.NewY(0f));
			}

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(targetPath);
			}
		}
	}
}