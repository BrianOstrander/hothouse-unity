namespace Lunra.Hothouse.Models
{
	public class DryingRackDefinition : BuildingDefinition
	{
		public override int MaximumOwners => 1;

		public override InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.ByIndividualWeight(
			(Inventory.Types.StalkRaw, 1),
			(Inventory.Types.StalkDry, 1)
		);

		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForJobs(
			Jobs.Stockpiler,
			Jobs.Smoker
		);

		public override Recipe[] Recipes => new[]
		{
			new Recipe(
				"Dry Stalks",
				Inventory.FromEntry(Inventory.Types.StalkRaw, 1),
				Inventory.FromEntry(Inventory.Types.StalkDry, 1) 
			), 
		};
	}
}