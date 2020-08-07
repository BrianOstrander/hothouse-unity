using System.Linq;

namespace Lunra.Hothouse.Models
{
	public class SweetGrassStockpileDefinition : BuildingDefinition
	{
		public override Inventory ConstructionInventory => Inventory.FromEntries(
			(Inventory.Types.Stalk, 4),
			(Inventory.Types.Grass, 2)
		);

		public override InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.ByIndividualWeight(
			(Inventory.Types.SweetGrass, 10)
		);

		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();

		public override InventoryDesire DefaultInventoryDesire => InventoryDesire.Ignored();

		public override string[] Tags => new[] {BuildingTags.Stockpile};

		public override GoalActivity[] Activities => new [] { GetDefaultEatActivity(Inventory.Types.SweetGrass) };
	}
}