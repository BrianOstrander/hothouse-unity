using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai
{
	public class InventoryRequestState<S, A> : BaseInventoryTransactionState<S, A>
		where S : AgentState<GameModel, A>
		where A : AgentModel
	{
		public override string Name => "InventoryRequest";
		protected override InventoryTransaction.Types TransactionType => InventoryTransaction.Types.Unknown;

		int timeouts;

		public override void OnInitialize()
		{
			AddTransitions(
				new ToReturnOnTimeout()	
			);
		}

		public override void Begin()
		{
			base.Begin();
			timeouts = 0;
		}

		public override void Idle()
		{
			timeouts++;
		}

		public class ToInventoryRequestOnPromises : AgentTransition<S, InventoryRequestState<S, A>, GameModel, A>
		{
			public override bool IsTriggered()
			{
				return Agent.InventoryPromises.Transactions.TryPeek(out var transaction) && transaction.State == InventoryTransaction.States.Request;
			}
		}

		class ToReturnOnTimeout : AgentTransition<InventoryRequestState<S, A>, S, GameModel, A>
		{
			public override bool IsTriggered() => 0 < SourceState.timeouts;

			public override void Transition() => Agent.InventoryPromises.Transactions.Pop();
		}
	}
}