using System.Linq;

namespace Lunra.Hothouse.Models
{
	public class SmokeRackDefinition : BuildingDefinition
	{
		public override int MaximumOwners => 1;

		public override InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.ByIndividualWeight(
			(Inventory.Types.StalkRaw, 1),
			(Inventory.Types.StalkDry, 1),
			(Inventory.Types.StalkSeed, 1),
			(Inventory.Types.StalkPop, 1)
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
				Inventory.FromEntry(Inventory.Types.StalkDry, 1),
				DayTime.FromHours(4f)
			),
			new Recipe(
				"Stalk Pop",
				Inventory.FromEntry(Inventory.Types.StalkSeed, 1),
				Inventory.FromEntry(Inventory.Types.StalkPop, 1),
				DayTime.FromHours(4f)
			),
		};
		public override Jobs[] WorkplaceForJobs => new[] {Jobs.Smoker};
	}
}