namespace Lunra.Hothouse.Models
{
	public struct HintCollection
	{
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