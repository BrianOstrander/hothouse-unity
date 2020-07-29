namespace Lunra.Hothouse.Models
{
	public class BonfireLightDefinition : LightBuildingDefinition
	{
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
		};
	}
}