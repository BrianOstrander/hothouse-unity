using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public class DwellerObligationState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "Obligation";

		string initialTargetId;
		IObligationModel target;
		
		public override void OnInitialize()
		{
			AddTransitions(
				new ToReturnOnTargetNull(),
				new ToReturnOnTargetIdMismatch(),
				new ToReturnOnObligationMissingFromTarget()
			);
		}

		void Reset()
		{
			initialTargetId = null;
			target = null;
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
							return m.Obligations.Value.Any(o => o.Id == Agent.Obligation.Value.ObligationId);
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
			public override bool IsTriggered() => SourceState.target.Obligations.Value.None(o => o.Id == Agent.Obligation.Value.ObligationId);

			public override void Transition()
			{
				SourceState.Reset();
				Agent.Obligation.Value = ObligationPromise.Default();
			}
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