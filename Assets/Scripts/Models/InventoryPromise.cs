using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public struct InventoryPromise
	{
		public static InventoryPromise Default() => new InventoryPromise(
			null,
			Operations.None,
			Inventory.Empty
		);
		
		public enum Operations
		{
			Unknown = 0,
			None = 10,
			ConstructionDeposit = 20,
			CleanupWithdrawal = 30
		}
		
		public readonly string TargetId;
		public readonly Operations Operation;
		public readonly Inventory Inventory;

		public InventoryPromise(
			string targetId,
			Operations operation,
			Inventory inventory
		)
		{
			TargetId = targetId;
			Operation = operation;
			Inventory = inventory;
		}

		public InventoryPromise NewInventory(Inventory inventory)
		{
			return new InventoryPromise(
				TargetId,
				Operation,
				inventory
			);
		}
	}
}