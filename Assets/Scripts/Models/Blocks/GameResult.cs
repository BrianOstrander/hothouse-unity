using Newtonsoft.Json;
namespace Lunra.Hothouse.Models
{
	public struct GameResult
	{
		public static GameResult Default() => new GameResult(States.Unknown, null, DayTime.Zero);
		
		public enum States
		{
			Unknown = 0,
			Displaying = 10,
			Failure = 20
		}

		[JsonProperty] public States State { get; private set; }
		[JsonProperty] public string Reason { get; private set; }
		[JsonProperty] public DayTime TimeSurvived { get; private set; }

		public GameResult(
			States state,
			string reason,
			DayTime timeSurvived
		)
		{
			State = state;
			Reason = reason;
			TimeSurvived = timeSurvived;
		}

		public GameResult New(States state)
		{
			return new GameResult(
				state,
				Reason,
				TimeSurvived
			);
		}
	}
}