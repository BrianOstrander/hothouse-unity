using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public struct InventoryPromise
	{
		public static InventoryPromise Default() => new InventoryPromise(null, Operations.None, Inventory.Empty);
		
		public enum Operations
		{
			Unknown = 0,
			None = 10,
			Construction = 20
		}
		
		public readonly string BuildingId;
		public readonly Operations Operation;
		public readonly Inventory Inventory;

		public InventoryPromise(
			string buildingId,
			Operations operation,
			Inventory inventory
		)
		{
			BuildingId = buildingId;
			Operation = operation;
			Inventory = inventory;
		}
	}
}