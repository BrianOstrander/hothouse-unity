namespace Lunra.Hothouse.Models
{
	public class BonfireLightDefinition : LightBuildingDefinition
	{
		public override Inventory ConstructionInventory => Inventory.FromEntries(
			(Inventory.Types.Stalk, 3)
		);
		
		public override GoalActivity[] Activities => new[]
		{
			new GoalActivity(
				GetActionName(Motives.Comfort),
				new []
				{
					(Motives.Comfort, -0.5f)
				},
				DayTime.FromMinutes(15f)
			),
			new GoalActivity(
				GetActionName(Motives.Sleep),
				new []
				{
					(Motives.Sleep, -0.33f),
					(Motives.Comfort, 0.5f),
					(Motives.Heal, -0.1f)
				},
				DayTime.FromHours(8f)
			), 
		};
	}
}