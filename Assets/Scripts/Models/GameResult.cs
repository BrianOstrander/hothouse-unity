namespace Lunra.Hothouse.Models
{
	public struct GameResult
	{
		public static GameResult Default() => new GameResult(States.Unknown, null);
		
		public enum States
		{
			Unknown = 0,
			Failure = 10
		}

		public readonly States State;
		public readonly string Reason;

		public GameResult(
			States state,
			string reason
		)
		{
			State = state;
			Reason = reason;
		}
	}
}