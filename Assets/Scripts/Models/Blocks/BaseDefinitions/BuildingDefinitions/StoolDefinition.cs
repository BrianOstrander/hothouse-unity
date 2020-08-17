namespace Lunra.Hothouse.Models
{
	public class StoolDefinition : BuildingDefinition
	{
		public override GoalActivity[] Activities => new[]
		{
			new GoalActivity(
				GetActionName(Motives.Sleep),
				new []
				{
					(Motives.Sleep, -0.1f),
					(Motives.Comfort, -0.1f)
				},
				DayTime.FromRealSeconds(30f)
			), 
			new GoalActivity(
				GetActionName("sit"),
				new []
				{
					(Motives.Comfort, 0.1f)
				},
				DayTime.FromRealSeconds(15f)
			)
		};
	}
}