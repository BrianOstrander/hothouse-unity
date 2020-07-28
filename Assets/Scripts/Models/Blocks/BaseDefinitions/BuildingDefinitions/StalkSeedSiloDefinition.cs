using System.Linq;
using Lunra.Core;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class StalkSeedSiloDefinition : BuildingDefinition
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
		
		public override int MaximumOwners => 1;

		public override bool IsFarm => true;
		public override Vector2 FarmSize => Vector2.one * 8f;
		public override Inventory.Types FarmSeed => Inventory.Types.StalkSeed;
		public override Jobs[] WorkplaceForJobs => new[] {Jobs.Farmer};
	}
}