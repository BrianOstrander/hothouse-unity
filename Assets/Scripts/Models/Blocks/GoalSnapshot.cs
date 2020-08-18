using Newtonsoft.Json;
namespace Lunra.Hothouse.Models
{
	public struct GoalSnapshot
	{
		[JsonProperty] public GoalResult Total { get; private set; }
		[JsonProperty] public (Motives Motive, GoalResult Value)[] Values { get; private set; }
			
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