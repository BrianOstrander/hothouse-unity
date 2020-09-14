using System.Collections.Generic;
using Lunra.Satchel;

namespace Lunra.Hothouse.Models
{
	public class StalkStockpileDefinition : BuildingDefinition
	{
		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();

		public override int GetCapacities(List<(int Count, PropertyFilter Filter)> capacities)
		{
			// TODO: Add a convenience function to this to make it easier to quickly define this tuple with a given
			// list of KV's 
			capacities.Add(
				(
					2,
					Game.Items.Builder
						.BeginPropertyFilter()
						.RequireAll(
							PropertyValidations.String.EqualTo(Items.Keys.Resource.Type, Items.Values.Resource.Types.Stalk)
						)
				)
			);
			
			capacities.Add(
				(
					2,
					Game.Items.Builder
						.BeginPropertyFilter()
						.RequireAll(
							PropertyValidations.String.EqualTo(Items.Keys.Resource.Type, Items.Values.Resource.Types.Scrap)
						)
				)
			);
			
			return 2;
		}
		
		public override string[] Tags => new[] {BuildingTags.Stockpile};
	}
}