using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class AttackMeleeHandlerState<S> : ObligationHandlerState<S, AttackMeleeHandlerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		static readonly Obligation[] obligationsHandled =
		{
			ObligationCategories.Attack.Melee
		};

		public override Obligation[] ObligationsHandled => obligationsHandled;

		public override void OnInitialize()
		{
			AddTransitions(
					
			);
		}
	}
}