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

		public override int MaximumOwners => 1;
		
		public override GoalActivity[] Activities => new[]
		{
			new GoalActivity(
				GetActionName(Motives.Eat),
				new []
				{
					(Motives.Eat, -0.5f)
				},
				DayTime.FromHours(1f),
				Inventory.FromEntry(Inventory.Types.StalkPop, 1)
			),
		};
		
		public override Jobs[] WorkplaceForJobs => new[] {Jobs.Stockpiler};
	}
}