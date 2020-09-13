using System.Collections.Generic;
using Lunra.Satchel;

namespace Lunra.Hothouse.Models
{
	public class StalkStockpileDefinition : BuildingDefinition
	{
		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();

		public override int GetCapacities(List<(int Count, PropertyFilter Filter)> capacities)
		{
			capacities.Add(
				(
					2,
					Game.Items.Builder
						.BeginPropertyFilter()
						.RequireAll(
							// PropertyValidation.Default	
						)
				)	
			);
			
			return 2;
		}
		
		public override string[] Tags => new[] {BuildingTags.Stockpile};
	}
}