using System.Linq;

namespace Lunra.Hothouse.Models
{
	public class BerryStockpileDefinition : BuildingDefinition
	{
		public override Inventory ConstructionInventory => Inventory.FromEntries(
			(Inventory.Types.Stalk, 3),
			(Inventory.Types.Grass, 2)
		);

		public override InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.ByIndividualWeight(
			(Inventory.Types.Berries, 10)
		);

		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();

		public override InventoryDesire DefaultInventoryDesire => InventoryDesire.Ignored();

		public override string[] Tags => new[] {BuildingTags.Stockpile};

		public override GoalActivity[] Activities => new [] { GetDefaultEatActivity(Inventory.Types.Berries) };
	}
}