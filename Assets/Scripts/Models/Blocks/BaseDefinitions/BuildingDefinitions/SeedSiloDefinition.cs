using System.Linq;
using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public class SeedSiloDefinition : BuildingDefinition
	{
		public override string DefaultPrefabId => "debug_small";

		public override InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.ByIndividualWeight(
			new []
			{
				Inventory.Types.StalkSeed	
			}
				.Select(t => (t, 25))
				.ToArray()
		);

		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.AllForJobs(
			Jobs.Stockpiler,
			Jobs.Farmer
		);
		
		public override int MaximumOwners => 2;
	}
}