using System.Collections.Generic;

namespace Lunra.Hothouse.Models
{
	public class StalkStockpileDefinition : BuildingDefinition
	{
		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();

		public override int Inventory(HashSet<string> resourceTypes)
		{
			// resourceTypes.Add(Items.Values.Resource.Types.Scrap);
			resourceTypes.Add(Items.Values.Resource.Types.Stalk);

			return 2;
		}
		
		public override string[] Tags => new[] {BuildingTags.Stockpile};
	}
}