using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai
{
	public class DwellerObligationState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "Obligation";

		DwellerTimeoutState<DwellerObligationState<S>> timeoutState;
			
		string initialTargetId;
		IObligationModel target;
		Obligation obligation;
		
		public override void OnInitialize()
		{
			AddChildStates(
				new DwellerNavigateState<DwellerObligationState<S>>(),
				timeoutState = new DwellerTimeoutState<DwellerObligationState<S>>()
			);
			
			AddTransitions(
				new ToReturnOnTargetNull(),
				new ToReturnOnTargetIdMismatch(),
				new ToReturnOnObligationMissingFromTarget(),
				new ToReturnOnObligationComplete(),
				
				new ToTimeoutForObligation(),
				new ToNavigateToObligation()
			);
		}

		void Reset()
		{
			initialTargetId = null;
			target = null;
			obligation = default;
		}

		void Cache()
		{
			obligation = target?.Obligations.Obligations.Value.FirstOrDefault(o => o.Id == Agent.Obligation.Value.ObligationId) ?? default;
		}

		public override void Begin()
		{
			if (!Agent.Obligation.Value.IsEnabled)
			{
				initialTargetId = null;
				target = null;
				Debug.LogWarning("Arrived in obligation state without an enabled obligation, this should not happen");
				return;
			}
			
			if (string.IsNullOrEmpty(initialTargetId))
			{
				// First time coming in with a fresh obligation...
				target = World.
					GetObligations(
						m =>
						{
							if (m.Id.Value != Agent.Obligation.Value.TargetId) return false;
							return m.Obligations.Obligations.Value.Any(o => o.Id == Agent.Obligation.Value.ObligationId);
						}
					)
					.FirstOrDefault();

				if (target == null)
				{
					Debug.LogWarning("Arrived in obligation state without being able to find a matching obligation, this should probably not happen");
					return;
				}

				initialTargetId = target.Id.Value;
			}
			
			Cache();
		}

		public override void Idle()
		{
			Cache();
		}

		class ToReturnOnTargetNull : AgentTransition<DwellerObligationState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => SourceState.target == null;

			public override void Transition()
			{
				SourceState.Reset();
				Agent.Obligation.Value = ObligationPromise.Default();
			}
		}
		
		class ToReturnOnTargetIdMismatch : AgentTransition<DwellerObligationState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => SourceState.initialTargetId != SourceState.target.Id.Value;

			public override void Transition()
			{
				SourceState.Reset();
				Agent.Obligation.Value = ObligationPromise.Default();
			}
		}
		
		class ToReturnOnObligationMissingFromTarget : AgentTransition<DwellerObligationState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => string.IsNullOrEmpty(SourceState.obligation.Id);

			public override void Transition()
			{
				SourceState.Reset();
				Agent.Obligation.Value = ObligationPromise.Default();
			}
		}
		
		class ToReturnOnObligationComplete : AgentTransition<DwellerObligationState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => SourceState.obligation.State == Obligation.States.Complete;

			public override void Transition()
			{
				SourceState.Reset();
				Agent.Obligation.Value = ObligationPromise.Default();
			}
		}
		
		protected class ToTimeoutForObligation : AgentTransition<DwellerObligationState<S>, DwellerTimeoutState<DwellerObligationState<S>>, GameModel, DwellerModel>
		{
			public override bool IsTriggered()
			{
				return SourceState.target.Enterable.Entrances.Value.Any(
					e => e.State == Entrance.States.Available && Vector3.Distance(Agent.Transform.Position.Value.NewY(0f), e.Position.NewY(0f)) < Agent.ObligationDistance.Value 
				);
			}

			public override void Transition()
			{
				Obligation get()
				{
					return SourceState.target.Obligations.Obligations.Value.FirstOrDefault(o => o.Id == Agent.Obligation.Value.ObligationId);
				}
				
				void set(Obligation newObligation)
				{
					SourceState.target.Obligations.Obligations.Value = SourceState.target.Obligations.Obligations.Value
						.Select(o => o.Id == newObligation.Id ? newObligation : o)
						.ToArray();
				}
				
				var obligation = get();
				
				var beginElapsed = 0f;
				var durationToElapse = 0f;

				switch (obligation.ConcentrationRequirement)
				{
					case Obligation.ConcentrationRequirements.Instant:
						durationToElapse = Agent.ObligationMinimumConcentrationDuration.Value;
						break;
					case Obligation.ConcentrationRequirements.Interruptible:
						beginElapsed = obligation.ConcentrationElapsed.Current; 
						durationToElapse = obligation.ConcentrationElapsed.Remaining;
						break;
					case Obligation.ConcentrationRequirements.NonInterruptible:
						durationToElapse = obligation.ConcentrationElapsed.Maximum;
						break;
					default:
						Debug.Log("Unrecognized ConcentrationRequirement: "+obligation.State);
						break;
				}
				
				SourceState.timeoutState.ConfigureForInterval(
					Interval.WithMaximum(Agent.ObligationMinimumConcentrationDuration.Value),
					delta => OnTimeoutUpdated(
						delta.Progress,
						delta.IsDone,
						beginElapsed,
						durationToElapse,
						get,
						set
					)
				);
				Agent.Desire.Value = Desires.None;
			}

			void OnTimeoutUpdated(
				float progress,
				bool isDone,
				float beginElapsed,
				float durationToElapse,
				Func<Obligation> get, 
				Action<Obligation> set
			)
			{
				var obligation = get();

				if (string.IsNullOrEmpty(obligation.Id)) return; // This can occur if the model with the obligation detects the completion and cleans up...
				if (obligation.State == Obligation.States.Complete) return; 

				var interval = obligation.ConcentrationElapsed;

				if (isDone) interval = interval.Done();
				else
				{
					interval = new Interval(
						beginElapsed + (progress * durationToElapse),
						beginElapsed + durationToElapse
					);
				}
			
				set(obligation.NewConcentrationElapsed(interval));
			}
		}
		
		class ToNavigateToObligation : AgentTransition<DwellerObligationState<S>, DwellerNavigateState<DwellerObligationState<S>>, GameModel, DwellerModel>
		{
			NavMeshPath path;
			
			public override bool IsTriggered()
			{
				var target = DwellerUtility.CalculateNearestAvailableEntrance(
					Agent.Transform.Position.Value,
					out path,
					out _,
					SourceState.target
				);

				return target != null;
			}

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(path);
		}
		
		/*
		class ToNavigateToObligation : AgentTransition<DwellerNavigateState<DwellerObligationState<S>>, GameModel, DwellerModel>
		{
			DwellerObligationState<S> sourceState;

			public ToNavigateToObligation(DwellerObligationState<S> sourceState) => this.sourceState = sourceState;

			public override bool IsTriggered()
			{
				if (!Agent.Obligation.Value.IsEnabled) return false;
				
			}
		}
		*/
	}
}