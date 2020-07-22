namespace Lunra.Hothouse.Models
{
	public class BedrollDefinition : BuildingDefinition
	{
		public override GoalActivity[] Activities => new[]
		{
			new GoalActivity(
				Type+".sleep",
				new []
				{
					(Motives.Sleep, -0.5f)
				},
				new DayTime(0.33f)
			), 
		};
	}
}