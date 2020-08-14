using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class DestroyGenericHandlerState<S> : ObligationHandlerState<S, DestroyGenericHandlerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		static readonly Obligation[] DefaultObligationsHandled =
		{
			ObligationCategories.Destroy.Generic
		};

		public override Obligation[] ObligationsHandled => DefaultObligationsHandled;

		Attack selectedAttack;
		
		public override void OnInitialize()
		{
			AddChildStates(
				new NavigateState(),
				TimeoutInstance = new TimeoutState()
			);
			
			AddTransitions(
				new ToReturnOnMissingObligation(),
				new ToReturnOnTimeout(),
				
				new ToTimeoutOnDestroyTarget(),
				
				new ToNavigateToDestroyTarget()
			);
		}

		public override void Begin()
		{
			base.Begin();
			
			if (CurrentCache.IsTargetNull) selectedAttack = null;
		}

		protected override float CalculateInteractionRadius(IObligationModel targetParent, Navigation.Result navigationResult)
		{
			if (!(targetParent is IHealthModel targetHealthParent))
			{
				Debug.LogError($"{Agent.ShortId} asked to attack a {targetParent.GetType().Name}, which does not implement {nameof(IHealthModel)}");
				return base.CalculateInteractionRadius(targetParent, navigationResult);
			}

			var targetDistance = targetParent.DistanceTo(Agent);

			var attackFound = Agent.Attacks.TryGetMostEffective(
				targetHealthParent,
				out selectedAttack,
				new FloatRange(
					Vector3.Distance(targetParent.Transform.Position.Value, navigationResult.Target),
					targetDistance
				),
				Game.SimulationTime.Value + CurrentCache.NavigationResult.CalculateNavigationTime(Agent.NavigationVelocity.Value)
			);

			// This should cause the agent to keep traveling to the obligation location or timeout...
			if (!attackFound) return -1f;

			return selectedAttack.Range.Maximum;
		}

		class ToTimeoutOnDestroyTarget : ToTimeoutOnTarget
		{
			bool isTargetDestroyed;

			protected override DayTime TimeoutDuration => SourceState.selectedAttack.Duration;

			public override bool IsTriggered()
			{
				if (!base.IsTriggered()) return false;
				return SourceState.selectedAttack != null;
			}

			protected override void OnTimeoutBegin()
			{
				switch (SourceState.CurrentCache.TargetParent)
				{
					case IHealthModel healthModel:
						isTargetDestroyed = SourceState.selectedAttack.Trigger(Game, Agent, healthModel).IsTargetDestroyed;
						TryPopObligation();
						break;
					default:
						Debug.LogError("Unrecognized target parent type: "+SourceState.CurrentCache.TargetParent.GetType());
						isTargetDestroyed = false;
						break;
				}
			}

			protected override bool CanPopObligation => isTargetDestroyed;
		}

		class ToNavigateToDestroyTarget : ToNavigateToTarget
		{
			public override void GetNavigationInterrupts(
				out NavigationPlan.Interrupts interrupt,
				out float radiusThreshold,
				out float pathThreshold
			)
			{
				if (SourceState.selectedAttack == null)
				{
					Debug.LogError("Tried to navigate but no attack has been selected");
					base.GetNavigationInterrupts(out interrupt, out radiusThreshold, out pathThreshold);
					return;
				}

				interrupt = NavigationPlan.Interrupts.LineOfSight | NavigationPlan.Interrupts.RadiusThreshold;
				radiusThreshold = SourceState.selectedAttack.Range.Maximum;
				pathThreshold = 0f;
			}
		}
	}
}