using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public abstract class LightBuildingDefinition : BuildingDefinition
	{
		public override FloatRange PlacementLightRequirement => new FloatRange(0.001f, 0.33f);
		public override Inventory LightFuel => Inventory.FromEntry(Inventory.Types.StalkDry, 1);
		public override Interval LightFuelInterval => Interval.WithMaximum(30f);
		public override LightStates LightState => LightStates.Fueled;

		public override Inventory DefaultInventory => LightFuel * 2;

		public override InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.ByIndividualWeight(DefaultInventory);

		public override InventoryDesire DefaultInventoryDesire => InventoryDesire.UnCalculated(DefaultInventoryCapacity.GetMaximum());

		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.DepositForJobs(
			Jobs.Stockpiler,
			Jobs.Laborer
		);
	}
}