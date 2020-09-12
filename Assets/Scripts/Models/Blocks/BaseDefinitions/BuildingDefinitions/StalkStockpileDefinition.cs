using System.Collections.Generic;

namespace Lunra.Hothouse.Models
{
	public class StalkStockpileDefinition : BuildingDefinition
	{
		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();

		public override int Inventory(List<string> resourceTypes)
		{
			resourceTypes.Add(Items.Values.Resource.Types.Stalk);
			resourceTypes.Add(Items.Values.Resource.Types.Scrap);

			return 3;
		}
		
		public override string[] Tags => new[] {BuildingTags.Stockpile};
	}
}