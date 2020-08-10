using System;

using Newtonsoft.Json;

using UnityEngine;
using UnityEngine.Serialization;

namespace Lunra.Core
{
	[Serializable]
	public struct FloatRange
	{
		public static FloatRange Zero => new FloatRange(0f, 0f);
		public static FloatRange Normal => new FloatRange(0f, 1f);
		public static FloatRange Constant(float value) => new FloatRange(value, value);

		[FormerlySerializedAs("x"), SerializeField, JsonProperty] float primary;
		[FormerlySerializedAs("y"), SerializeField, JsonProperty] float secondary;

		[JsonIgnore] public float Primary => primary;

		[JsonIgnore] public float Secondary => secondary;

		[JsonIgnore] public float Delta => Secondary - Primary;

		[JsonIgnore] public float Maximum => Mathf.Max(Primary, Secondary);
		
		[JsonIgnore] public float Minimum => Mathf.Min(Primary, Secondary);
		
		public FloatRange(float primary, float secondary)
		{
			this.primary = primary;
			this.secondary = secondary;
		}

		public FloatRange NewPrimary(float primary) { return new FloatRange(primary, Secondary); }
		public FloatRange NewSecondary(float secondary) { return new FloatRange(Primary, secondary); }

		/// <summary>
		/// Takes a value between 0.0 and 1.0 and returns where that value would
		/// fall in a range between the Primary and Secondary values.
		/// </summary>
		/// <returns>The evaluate.</returns>
		/// <param name="normal">Normal.</param>
		public float Evaluate(float normal) { return Primary + (Delta * normal); }

		/// <summary>
		/// Takes a value that should be normalized, but may not be between 0.0
		/// and 1.0, and clamps it to a normal range before evaluating it
		/// between the Primary and Secondary values.
		/// </summary>
		/// <returns>The clamped.</returns>
		/// <param name="normal">Normal.</param>
		public float EvaluateClamped(float normal) { return Evaluate(Mathf.Clamp01(normal)); }

		/// <summary>
		/// Takes a value and finds its unclamped normal between the Primary and
		/// Secondary values. May not return a value between 0.0 and 1.0.
		/// </summary>
		/// <returns>The progress.</returns>
		/// <param name="value">Value.</param>
		public float Progress(float value)
		{
			if (Mathf.Approximately(0f, Delta)) return 1f;
			return (value - primary) / Delta;
		}

		/// <summary>
		/// Takes a value and finds its clamped normal between the Primary and
		/// Secondary values. Will always return a value between 0.0 and 1.0.
		/// </summary>
		/// <returns>The clamped.</returns>
		/// <param name="value">Value.</param>
		public float ProgressClamped(float value)
		{
			return Progress(
				Mathf.Clamp(
					value,
					Mathf.Min(Primary, Secondary),
					Mathf.Max(Primary, Secondary)
				)
			);
		}
		
		public static implicit operator FloatRange(Vector2 v)
		{
			return new FloatRange(v.x, v.y);
		}
		
		public static implicit operator Vector2(FloatRange f)
		{
			return new Vector2(f.Primary, f.Secondary);
		}
	}
}