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

		public States State { get; }
		public string Reason { get; }
		public DayTime TimeSurvived { get; }

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