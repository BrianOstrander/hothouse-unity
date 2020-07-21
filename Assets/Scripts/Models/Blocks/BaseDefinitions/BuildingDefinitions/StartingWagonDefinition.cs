using System.Linq;
using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public class StartingWagonDefinition : BuildingDefinition
	{
		public override InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.ByIndividualWeight(
			new []
			{
				Inventory.Types.StalkSeed,
				Inventory.Types.StalkRaw,
				Inventory.Types.StalkDry,
				Inventory.Types.StalkPop
			}
				.Select(t => (t, 25))
				.ToArray()
		);

		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForAnyJob();

		public override int MaximumOwners => 2;
	}
}