using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class DestroyMeleeHandlerState<S> : ObligationHandlerState<S, DestroyMeleeHandlerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		static readonly Obligation[] DefaultObligationsHandled =
		{
			ObligationCategories.Destroy.Melee
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
				
				new ToTimeoutOnAttackTarget(),
				
				new ToNavigateToTarget()
			);
		}

		public override void Begin()
		{
			selectedAttack = null;
			
			base.Begin();

			if (CurrentCache.IsTargetNull) return;
			
			
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
				out var attack,
				new FloatRange(
					Vector3.Distance(targetParent.Transform.Position.Value, navigationResult.Target),
					targetDistance
				),
				Game.SimulationTime.Value + CurrentCache.NavigationResult.CalculateNavigationTime(Agent.NavigationVelocity.Value)
			);

			if (!attackFound)
			{
				Debug.LogError($"{Agent.ShortId} asked to attack a {targetParent.GetType().Name}, but no valid attack was available");
				return base.CalculateInteractionRadius(targetParent, navigationResult);
			}

			return Mathf.Min(targetDistance, attack.Range.Maximum);
		}

		class ToTimeoutOnAttackTarget : ToTimeoutOnTarget
		{
			bool isTargetDestroyed;
			
			protected override void OnTimeoutEnd()
			{
				switch (SourceState.CurrentCache.TargetParent)
				{
					case IHealthModel healthModel:
						
						Debug.LogWarning("TODO DAMAGE HERE");
						// var result = Damage.Apply(
						// 	Damage.Types.Generic,
						// 	Agent.MeleeDamage.Value,
						// 	Agent,
						// 	healthModel
						// );

						// isTargetDestroyed = result.IsTargetDestroyed;
						break;
					default:
						Debug.LogError("Unrecognized target parent type: "+SourceState.CurrentCache.TargetParent.GetType());
						isTargetDestroyed = false;
						break;
				}
			}

			protected override bool CanPopObligation => isTargetDestroyed;
		}
	}
}