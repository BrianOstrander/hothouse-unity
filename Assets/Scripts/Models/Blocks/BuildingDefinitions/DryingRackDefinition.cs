namespace Lunra.Hothouse.Models
{
	public class DryingRackDefinition : BuildingDefinition
	{
		public override string PrefabId => "debug_building";

		protected override int MaximumOwners => 1;

		protected override InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.ByIndividualWeight(
			(Inventory.Types.StalkRaw, 1),
			(Inventory.Types.StalkDry, 1)
		);

		protected override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForJobs(
			Jobs.Stockpiler,
			Jobs.Smoker
		);

		protected override Recipe[] Recipes => new[]
		{
			new Recipe(
				"Dry Stalks",
				Inventory.FromEntry(Inventory.Types.StalkRaw, 1),
				Inventory.FromEntry(Inventory.Types.StalkDry, 1) 
			), 
		};
	}
}