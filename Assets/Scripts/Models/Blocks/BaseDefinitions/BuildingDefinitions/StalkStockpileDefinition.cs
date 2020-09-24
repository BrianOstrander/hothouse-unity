using System.Collections.Generic;
using Lunra.Core;
using Lunra.Satchel;

namespace Lunra.Hothouse.Models
{
	public class StalkStockpileDefinition : BuildingDefinition
	{
		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();

		public override void GetCapacities(CapacityPoolBuilder capacityPoolBuilder)
		{
			capacityPoolBuilder
				.Pool(
					Items.Values.CapacityPool.Types.Construction,
					2,
					(Items.Values.Resource.Types.Stalk, 2)
				)
				.Pool(
					Items.Values.CapacityPool.Types.Stockpile,
					4,
					Items.Values.Resource.Types.Stalk
				);
		}
		
		public override string[] Tags => new[] {BuildingTags.Stockpile};
	}
}