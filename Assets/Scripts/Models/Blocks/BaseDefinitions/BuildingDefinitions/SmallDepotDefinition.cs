using System.Linq;
using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public class SmallDepotDefinition : BuildingDefinition
	{
		protected override InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.ByIndividualWeight(
			EnumExtensions.GetValues(Inventory.Types.Unknown)
				.Select(t => (t, 25))
				.ToArray()
		);

		protected override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();
		
		protected override int MaximumOwners => 2;
	}
}