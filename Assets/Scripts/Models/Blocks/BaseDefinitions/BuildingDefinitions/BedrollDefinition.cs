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
					(Motives.Comfort, -0.1f),
					(Motives.Heal, -0.5f)
				},
				DayTime.FromHours(8f),
				requiresOwnership: true
			), 
		};

		public override int MaximumOwners => 1;

		public override string[] Tags => new[] { BuildingTags.Bed };
	}
}