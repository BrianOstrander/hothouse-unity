using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public struct InventoryPromiseOld
	{
		public static InventoryPromiseOld Default() => new InventoryPromiseOld(
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

		public InventoryPromiseOld(
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

		public InventoryPromiseOld NewInventory(Inventory inventory)
		{
			return new InventoryPromiseOld(
				Source,
				Target,
				Operation,
				inventory
			);
		}

		public override string ToString() => Source + " -> " + Target + "\nOperation: " + Operation + "\nInventory " + Inventory;
	}
}