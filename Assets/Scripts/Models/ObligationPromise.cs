namespace Lunra.Hothouse.Models
{
	public struct ObligationPromise
	{
		public static ObligationPromise Default() => new ObligationPromise(false, null, null);

		public static ObligationPromise New(
			string targetId,
			string obligationId
		)
		{
			return new ObligationPromise(
				true,
				targetId,
				obligationId
			);
		}
		
		public readonly bool IsEnabled;
		public readonly string TargetId;
		public readonly string ObligationId;

		ObligationPromise(
			bool isEnabled,
			string targetId,
			string obligationId
		)
		{
			IsEnabled = isEnabled;
			TargetId = targetId;
			ObligationId = obligationId;
		}
	}
}