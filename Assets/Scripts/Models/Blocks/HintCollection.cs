namespace Lunra.Hothouse.Models
{
	public struct HintCollection
	{
		public static HintCollection NewDelay(float timeoutDuration)
		{
			return New(
				Hint.NewDelay(timeoutDuration)
			);
		}
		
		public static HintCollection New(
			params Hint[] hints
		)
		{
			return new HintCollection(
				HintStates.Idle,
				hints
			);
		}
		
		public HintStates State;
		public Hint[] Hints;

		HintCollection(
			HintStates state,
			params Hint[] hints
		)
		{
			State = state;
			Hints = hints;
		}
	}
}