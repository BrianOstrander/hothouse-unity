namespace Lunra.Hothouse.Models
{
	public class StalkStockpileDefinition : BuildingDefinition
	{
		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();

		public override StackRecipe[] Inventory => new[]
		{
			Items.Instantiate.Capacity
				.Of(Items.Values.Resource.Ids.Stalk, 10)
				.ToSingleStackRecipe()
		};
		
		public override string[] Tags => new[] {BuildingTags.Stockpile};
	}
}