namespace Lunra.Hothouse.Models
{
	public class GrassStockpileDefinition : BuildingDefinition
	{
		public override InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.ByIndividualWeight(
			(Inventory.Types.Grass, 10)
		);

		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();

		public override InventoryDesire DefaultInventoryDesire => InventoryDesire.Ignored();

		public override string[] Tags => new[] {BuildingTags.Stockpile};
	}
}