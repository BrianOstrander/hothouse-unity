using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public abstract class LightBuildingDefinition : BuildingDefinition
	{
		protected override FloatRange PlacementLightRequirement => new FloatRange(0.001f, 0.33f);
		protected override Inventory LightFuel => Inventory.FromEntry(Inventory.Types.StalkDry, 1);
		protected override Interval LightFuelInterval => Interval.WithMaximum(30f);
		protected override LightStates LightState => LightStates.Fueled;

		protected override Inventory DefaultInventory => LightFuel * 2;

		protected override InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.ByIndividualWeight(DefaultInventory);

		protected override InventoryDesire DefaultInventoryDesire => InventoryDesire.UnCalculated(DefaultInventoryCapacity.GetMaximum());

		protected override InventoryPermission DefaultInventoryPermission => InventoryPermission.DepositForJobs(
			Jobs.Stockpiler,
			Jobs.Laborer
		);

	}
}