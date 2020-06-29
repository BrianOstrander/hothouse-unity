using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai
{
	public abstract class BaseInventoryTransactionState<S, A> : AgentState<GameModel, A>
		where S : AgentState<GameModel, A>
		where A : AgentModel 
	{
		protected abstract InventoryTransaction.Types TransactionType { get; }
		
		protected InventoryTransaction CurrentTransaction { get; private set; }

		public override void Begin()
		{
			if (!Agent.InventoryPromises.Transactions.TryPeek(out var currentTransaction)) return;
			CurrentTransaction = currentTransaction;
		}

		class ToReturnOnMissingTransaction : AgentTransition<BaseInventoryTransactionState<S, A>, S, GameModel, A>
		{
			public override bool IsTriggered() => SourceState.CurrentTransaction == null;
		}
		
		class ToReturnOnTypeMismatch : AgentTransition<BaseInventoryTransactionState<S, A>, S, GameModel, A>
		{
			public override bool IsTriggered() => SourceState.CurrentTransaction.Type != SourceState.TransactionType;
		}
	}
}