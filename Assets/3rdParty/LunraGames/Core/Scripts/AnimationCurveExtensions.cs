using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace LunraGames
{
	public static class AnimationCurveExtensions
	{
		/// <summary>
		/// Used to see if two curves, possibly null, are equal to each other.
		/// </summary>
		/// <remarks>
		/// The default equality for curves is not very good, probably suffering from floating point issues, so we use 
		/// this more robust method instead.
		/// </remarks>
		/// <returns><c>true</c>, if equal was curvesed, <c>false</c> otherwise.</returns>
		/// <param name="curve0">Curve0.</param>
		/// <param name="curve1">Curve1.</param>
		public static bool CurvesEqual(AnimationCurve curve0, AnimationCurve curve1)
		{
			if (curve0 == null && curve1 == null) return true;
			if ((curve0 == null && curve1 != null) || (curve0 != null && curve1 == null)) return false;

			if (curve0.keys.Length != curve1.keys.Length) return false;

			var pairedKeys = new List<Keyframe>(curve0.keys);
			foreach (var key in curve1.keys)
			{
				int? index = null;
				for (var i = 0; i < pairedKeys.Count; i++)
				{
					var k = pairedKeys[i];
					if (Mathf.Approximately(key.inTangent, k.inTangent) && Mathf.Approximately(key.outTangent, k.outTangent) && Mathf.Approximately(key.time, k.time) && Mathf.Approximately(key.value, k.value))
					{
						index = i;
					}
					if (index.HasValue) break;
				}
				if (index.HasValue) pairedKeys.RemoveAt(index.Value);
				else return false;
			}
			return true;
		}

		public static float MinimumKey(this AnimationCurve curve)
		{
			return curve.keys.Min(k => k.value);
		}

		public static float MaximumKey(this AnimationCurve curve)
		{
			return curve.keys.Max(k => k.value);
		}

		public static float AverageKey(this AnimationCurve curve)
		{
			return curve.keys.Average(k => k.value);
		}

		public static float MinimumSample(this AnimationCurve curve, int count)
		{
			return curve.Samples(count).Min();
		}

		public static float MaximumSample(this AnimationCurve curve, int count)
		{
			return curve.Samples(count).Max();
		}

		public static float AverageSample(this AnimationCurve curve, int count)
		{
			return curve.Samples(count).Average();
		}

		public static IEnumerable<float> Samples(this AnimationCurve curve, int count)
		{
			var samples = new List<float> {};

			if (curve.keys == null || curve.length == 0) return samples;

			var minTime = curve.keys.Min(k => k.time);
			var maxTime = curve.keys.Max(k => k.time);
			var delta = maxTime - minTime;
			var sampleRange = delta / count;
			var sampleOffset = sampleRange * 0.5f;
			var currSampleTime = minTime + sampleOffset;
			for (var i = 0; i < count; i++)
			{
				samples.Add(curve.Evaluate(currSampleTime));
				currSampleTime += sampleRange;
			}
			return samples;
		}

		public static AnimationCurve Constant(float value = 0f)
		{
			return AnimationCurve.Linear(0f, value, 1f, value);
		}

		public static AnimationCurve LinearNormal()
		{
			return AnimationCurve.Linear(0f, 0f, 1f, 1f);
		}

		public static AnimationCurve LinearInverseNormal()
		{
			return AnimationCurve.Linear(0f, 1f, 1f, 0f);
		}
	}
}