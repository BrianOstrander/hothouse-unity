namespace Lunra.Hothouse.Models
{
	public class StalkStockpileDefinition : BuildingDefinition
	{
		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();

		public override StackRecipe[] Inventory => new[]
		{
			Items.Instantiate.Capacity
				.CacheOf(Items.Values.Resource.Types.Stalk, 10)
				.ToSingleStackRecipe()
		};
		
		public override string[] Tags => new[] {BuildingTags.Stockpile};
	}
}