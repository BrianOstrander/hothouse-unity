namespace Lunra.Hothouse.Models
{
	public class StalkStockpileDefinition : BuildingDefinition
	{
		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();

		// public override StackRecipe[] Inventory => new[]
		// {
		// 	new StackRecipe(
		// 		10,
		// 		
		// 	), 
		// };

		public override string[] Tags => new[] {BuildingTags.Stockpile};
	}
}