using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class SleepDesireState : DesireState<SleepDesireState>
	{
		public override Desires Desire => Desires.Sleep;

		public override void Begin()
		{
			TryGetBed(out _);
		}

		protected override bool ValidateDesireBuilding(BuildingModel building) => building.Ownership.Contains(Agent.Id.Value);

		bool TryGetBed(out BuildingModel bed)
		{
			bed = null;
			if (Agent.Bed.Value.TryGetInstance(Game, out bed)) return true;

			bed = Game.Buildings.AllActive
				.FirstOrDefault(
					m =>
					{
						if (!m.IsBuildingState(BuildingStates.Operating)) return false;
						if (m.Ownership.IsFull) return false;
						if (m.Enterable.Entrances.Value.None(e => e.State == Entrance.States.Available)) return false;
						return 0f < m.DesireQualities.Value.FirstAvailableQualityOrDefault(Desire);
					}
				);

			if (bed == null) return false;

			bed.Ownership.Claimers.Value = bed.Ownership.Claimers.Value
				.Append(InstanceId.New(Agent))
				.ToArray();

			Agent.Bed.Value = InstanceId.New(bed);
			
			return true;
		}
	}
}