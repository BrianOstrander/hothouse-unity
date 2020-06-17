using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class ObligationState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "Obligation";

		TimeoutState<ObligationState<S>> timeoutState;
			
		string initialTargetId;
		IObligationModel target;
		Obligation obligation;
		
		public override void OnInitialize()
		{
			AddChildStates(
				new NavigateState<ObligationState<S>>(),
				timeoutState = new TimeoutState<ObligationState<S>>()
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
			obligation = target?.Obligations.All.Value.FirstOrDefault(o => o.PromiseId == Agent.Obligation.Value.ObligationPromiseId) ?? default;
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
				target = Game.
					GetObligations(
						m =>
						{
							if (m.Id.Value != Agent.Obligation.Value.TargetId) return false;
							return m.Obligations.All.Value.Any(o => o.PromiseId == Agent.Obligation.Value.ObligationPromiseId);
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

		class ToReturnOnTargetNull : AgentTransition<ObligationState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => SourceState.target == null;

			public override void Transition()
			{
				SourceState.Reset();
				Agent.Obligation.Value = ObligationPromise.Default();
			}
		}
		
		class ToReturnOnTargetIdMismatch : AgentTransition<ObligationState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => SourceState.initialTargetId != SourceState.target.Id.Value;

			public override void Transition()
			{
				SourceState.Reset();
				Agent.Obligation.Value = ObligationPromise.Default();
			}
		}
		
		class ToReturnOnObligationMissingFromTarget : AgentTransition<ObligationState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => string.IsNullOrEmpty(SourceState.obligation.PromiseId);

			public override void Transition()
			{
				SourceState.Reset();
				Agent.Obligation.Value = ObligationPromise.Default();
			}
		}
		
		class ToReturnOnObligationComplete : AgentTransition<ObligationState<S>, S, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => SourceState.obligation.State == Obligation.States.Complete;

			public override void Transition()
			{
				SourceState.Reset();
				Agent.Obligation.Value = ObligationPromise.Default();
			}
		}
		
		protected class ToTimeoutForObligation : AgentTransition<ObligationState<S>, TimeoutState<ObligationState<S>>, GameModel, DwellerModel>
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
					return SourceState.target.Obligations.All.Value.FirstOrDefault(o => o.PromiseId == Agent.Obligation.Value.ObligationPromiseId);
				}
				
				void set(Obligation newObligation)
				{
					SourceState.target.Obligations.All.Value = SourceState.target.Obligations.All.Value
						.Select(o => o.PromiseId == newObligation.PromiseId ? newObligation : o)
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

				if (string.IsNullOrEmpty(obligation.PromiseId)) return; // This can occur if the model with the obligation detects the completion and cleans up...
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
		
		class ToNavigateToObligation : AgentTransition<ObligationState<S>, NavigateState<ObligationState<S>>, GameModel, DwellerModel>
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
	}
}