using System;
using UnityEngine;
using Lunra.NumberDemon;

namespace Lunra.WildVacuum.Models
{
	public struct Interval
	{
		public static Interval WithMaximum(float maximum) => new Interval(0f, maximum);

		public static Interval WithRandomMaximum(float maximum) => WithRandomMaximum(0f, maximum);
		
		public static Interval WithRandomMaximum(float minimum, float maximum)
		{
			return new Interval(
				DemonUtility.GetNextFloat(minimum, maximum),
				maximum
			);
		}
			
		public readonly float Current;
		public readonly float Maximum;
		public readonly float Normalized;
		public readonly bool IsDone;

		Interval(
			float current,
			float maximum
		)
		{
			if (current < 0f) throw new ArgumentOutOfRangeException(nameof(current), "Must be greater than zero");
			if (maximum < current) throw new ArgumentOutOfRangeException(nameof(maximum), "Must be greater than or equal to "+nameof(current));
			
			Current = current;
			Maximum = maximum;
			Normalized = Mathf.Approximately(maximum, 0f) ? 1f : current / maximum;
			IsDone = Mathf.Approximately(current, maximum);
		}

		public Interval Update(float time)
		{
			return new Interval(
				Mathf.Min(time + Current, Maximum),
				Maximum
			);
		}
	}
}