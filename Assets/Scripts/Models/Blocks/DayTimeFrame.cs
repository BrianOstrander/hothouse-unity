using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public struct DayTimeFrame
	{
		public static DayTimeFrame Zero => new DayTimeFrame(0f, 0f);
		public static DayTimeFrame Maximum => new DayTimeFrame(0f, 1f);
		
		public readonly float Begin;
		public readonly float End;
		public readonly bool NoValidFrame;
		public readonly bool Inverted;

		public DayTimeFrame(
			float begin,
			float end
		)
		{
			Begin = Mathf.Clamp01(begin);
			End = Mathf.Clamp01(end);
			NoValidFrame = Mathf.Approximately(Begin, End);
			Inverted = End < Begin;
		}

		public bool Contains(DayTime dayTime)
		{
			if (NoValidFrame) return false;

			if (Inverted)
			{
				return dayTime.Time < End || Begin < dayTime.Time;
			}

			return Begin < dayTime.Time && dayTime.Time < End;
		}
	}
}