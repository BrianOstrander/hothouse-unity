using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public struct InventoryPromise
	{
		public static InventoryPromise Default() => new InventoryPromise(
			InstanceId.Null(),
			InstanceId.Null(),
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
		
		public readonly InstanceId Source;
		public readonly InstanceId Target;
		public readonly Operations Operation;
		public readonly Inventory Inventory;

		public InventoryPromise(
			InstanceId source,
			InstanceId target,
			Operations operation,
			Inventory inventory
		)
		{
			Source = source;
			Target = target;
			Operation = operation;
			Inventory = inventory;
		}

		public InventoryPromise NewInventory(Inventory inventory)
		{
			return new InventoryPromise(
				Source,
				Target,
				Operation,
				inventory
			);
		}

		public override string ToString() => Source + " -> " + Target + "\nOperation: " + Operation + "\nInventory " + Inventory;
	}
}