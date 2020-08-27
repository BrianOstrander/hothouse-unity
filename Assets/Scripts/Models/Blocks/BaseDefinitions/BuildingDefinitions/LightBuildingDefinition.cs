using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public abstract class LightBuildingDefinition : BuildingDefinition
	{
		public override FloatRange PlacementLightRequirement => new FloatRange(0f, 0.33f);
		public override Interval LightFuelInterval => Interval.WithMaximum(2f);
		public override LightStates LightState => LightStates.Fueled;

		public override InventoryPermission DefaultInventoryPermission => InventoryPermission.DepositForJobs(
			Jobs.Stockpiler,
			Jobs.Laborer
		);
	}
}