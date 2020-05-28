namespace Lunra.Hothouse.Models
{
	public struct ObligationPromise
	{
		public static ObligationPromise Default() => new ObligationPromise(false, null, null);

		public static ObligationPromise New(
			string targetId,
			string obligationPromiseId
		)
		{
			return new ObligationPromise(
				true,
				targetId,
				obligationPromiseId
			);
		}
		
		public readonly bool IsEnabled;
		public readonly string TargetId;
		public readonly string ObligationPromiseId;

		ObligationPromise(
			bool isEnabled,
			string targetId,
			string obligationPromiseId
		)
		{
			IsEnabled = isEnabled;
			TargetId = targetId;
			ObligationPromiseId = obligationPromiseId;
		}
	}
}