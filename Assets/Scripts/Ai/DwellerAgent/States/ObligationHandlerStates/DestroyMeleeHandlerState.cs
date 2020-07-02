using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class DestroyMeleeHandlerState<S> : ObligationHandlerState<S, DestroyMeleeHandlerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		static readonly Obligation[] obligationsHandled =
		{
			ObligationCategories.Destroy.Melee
		};

		public override Obligation[] ObligationsHandled => obligationsHandled;

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
			protected override bool CanPopObligation
			{
				get
				{
					switch (SourceState.CurrentCache.TargetParent)
					{
						case IHealthModel healthModel:
							var result = Damage.Apply(
								Damage.Types.Generic,
								Agent.MeleeDamage.Value,
								Agent,
								healthModel
							);

							return result.IsTargetDestroyed;
						default:
							Debug.LogError("Unrecognized target parent type: "+SourceState.CurrentCache.TargetParent.GetType());
							return false;
					}
				}
			}
		}
	}
}