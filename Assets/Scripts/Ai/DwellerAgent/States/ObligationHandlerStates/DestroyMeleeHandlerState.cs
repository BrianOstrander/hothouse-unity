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