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
			FloatRange discontentRange,
			float? discontentNormal = null
		)
		{
			Insistence = insistence;
			Discontent = discontent;
			DiscontentRange = discontentRange;
			DiscontentNormal = discontentNormal ?? DiscontentRange.ProgressClamped(Discontent);
		}

		public GoalResult New(float insistence)
		{
			return new GoalResult(
				insistence,
				-1f,
				DiscontentRange,
				-1f
			);
		}
	}
}