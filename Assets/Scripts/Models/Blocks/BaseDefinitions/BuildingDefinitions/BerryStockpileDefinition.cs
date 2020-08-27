using System.Linq;

namespace Lunra.Hothouse.Models
{
	public class BerryStockpileDefinition : BuildingDefinition
	{
		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();

		public override string[] Tags => new[] {BuildingTags.Stockpile};

		// public override GoalActivity[] Activities => new [] { GetDefaultEatActivity(Inventory.Types.Berries) };
	}
}