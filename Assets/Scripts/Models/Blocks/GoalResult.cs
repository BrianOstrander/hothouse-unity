using Newtonsoft.Json;
using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public struct GoalResult
	{
		[JsonProperty] public float Insistence { get; private set; }
		[JsonProperty] public float Discontent { get; private set; }
		[JsonProperty] public FloatRange DiscontentRange { get; private set; }
		[JsonProperty] public float DiscontentNormal { get; private set; }

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
				Discontent,
				DiscontentRange,
				DiscontentNormal
			);
		}
	}
}