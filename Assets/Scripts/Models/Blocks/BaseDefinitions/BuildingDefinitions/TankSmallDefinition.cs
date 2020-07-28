using System.Linq;
using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public class TankSmallDefinition : BuildingDefinition
	{
		public override InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.ByIndividualWeight(
			(Inventory.Types.Water, 10)
		);

		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();
		
		public override int MaximumOwners => 1;

		public override Jobs[] WorkplaceForJobs => new[] {Jobs.Stockpiler};
	}
}