using System.Linq;
using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public class DepotSmallDefinition : BuildingDefinition
	{
		public override InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.ByIndividualWeight(
			EnumExtensions.GetValues(Inventory.Types.Unknown)
				.Select(t => (t, 25))
				.ToArray()
		);

		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();
		
		public override int MaximumOwners => 2;
	}
}