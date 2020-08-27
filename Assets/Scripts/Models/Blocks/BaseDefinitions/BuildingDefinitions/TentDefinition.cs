namespace Lunra.Hothouse.Models
{
	public class TentDefinition : BuildingDefinition
	{
		public override GoalActivity[] Activities => new[]
		{
			new GoalActivity(
				GetActionName(Motives.Sleep),
				new []
				{
					(Motives.Sleep, -0.65f),
					(Motives.Comfort, -0.2f),
					(Motives.Heal, -0.75f)
				},
				DayTime.FromHours(8f),
				requiresOwnership: true
			), 
		};

		public override int MaximumOwners => 1;

		public override string[] Tags => new[] { BuildingTags.Bed };
	}
}