using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public struct GoalResult
	{
		public float Insistence { get; }
		public float Discontent { get; }
		public FloatRange DiscontentRange { get; }
		public float DiscontentNormal { get; }

		public GoalResult(
			float insistence,
			float discontent,
			FloatRange discontentRange
		)
		{
			Insistence = insistence;
			Discontent = discontent;
			DiscontentRange = discontentRange;
			DiscontentNormal = DiscontentRange.ProgressClamped(Discontent);
		}
	}
}