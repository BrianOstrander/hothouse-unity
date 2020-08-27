namespace Lunra.Hothouse.Models
{
	public class ScrapStockpileDefinition : BuildingDefinition
	{
		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();

		public override string[] Tags => new[] {BuildingTags.Stockpile};
	}
}