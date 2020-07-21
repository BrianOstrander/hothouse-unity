namespace Lunra.Hothouse.Models
{
	public struct GoalSnapshot
	{
		public GoalResult Total { get; }
		public (Motives Motive, GoalResult Value)[] Values { get; }
			
		public GoalSnapshot(
			GoalResult total,
			(Motives Motive, GoalResult Value)[] values
		)
		{
			Total = total;
			Values = values;
		}
	}
}