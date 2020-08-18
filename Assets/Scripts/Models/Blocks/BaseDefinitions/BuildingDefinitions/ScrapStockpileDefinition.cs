namespace Lunra.Hothouse.Models
{
	public class ScrapStockpileDefinition : BuildingDefinition
	{
		public override Inventory ConstructionInventory => Inventory.FromEntries(
			(Inventory.Types.Stalk, 4)
		);

		public override InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.ByIndividualWeight(
			(Inventory.Types.Scrap, 10)
		);

		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();

		public override InventoryDesire DefaultInventoryDesire => InventoryDesire.Ignored();

		public override string[] Tags => new[] {BuildingTags.Stockpile};
	}
}