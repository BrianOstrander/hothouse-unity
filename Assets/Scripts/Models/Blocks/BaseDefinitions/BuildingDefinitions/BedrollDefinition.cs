namespace Lunra.Hothouse.Models
{
	public class BedrollDefinition : BuildingDefinition
	{
		public override GoalActivity[] Activities => new[]
		{
			new GoalActivity(
				GetActionName(Motives.Sleep),
				new []
				{
					(Motives.Sleep, -0.5f),
					(Motives.Heal, -0.2f)
				},
				DayTime.FromHours(8f)
			), 
		};
	}
}