/*
using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai
{
	public abstract class BaseInventoryTransactionState<S, A> : AgentState<GameModel, A>
		where S : AgentState<GameModel, A>
		where A : AgentModel 
	{
		

		public override void Begin()
		{
			Agent.InventoryPromises.Transactions.TryPeek(out var currentTransaction);
			CurrentTransaction = currentTransaction;
		}
		
		protected void PopPush(InventoryTransaction transaction)
		{
			Agent.InventoryPromises.Transactions.Pop();
			Agent.InventoryPromises.Transactions.Push(
				CurrentTransaction = transaction
			);
		}
		
		// protected bool Get

	}
}
*/