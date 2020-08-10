using System;

using Newtonsoft.Json;

using UnityEngine;
using UnityEngine.Serialization;

namespace Lunra.Core
{
	[Serializable]
	public struct IntegerRange
	{
		public static IntegerRange Zero => new IntegerRange(0, 0);
		public static IntegerRange Normal => new IntegerRange(0, 1);
		public static IntegerRange Constant(int value) => new IntegerRange(value, value);

		[FormerlySerializedAs("x"), SerializeField, JsonProperty] int primary;
		[FormerlySerializedAs("y"), SerializeField, JsonProperty] int secondary;

		[JsonIgnore] public int Primary => primary;

		[JsonIgnore] public int Secondary => secondary;

		[JsonIgnore] public int Delta => Secondary - Primary;

		[JsonIgnore] public int Maximum => Mathf.Max(Primary, Secondary);
		
		[JsonIgnore] public int Minimum => Mathf.Min(Primary, Secondary);
		
		public IntegerRange(int primary, int secondary)
		{
			this.primary = primary;
			this.secondary = secondary;
		}

		public IntegerRange NewPrimary(int primary) { return new IntegerRange(primary, Secondary); }
		public IntegerRange NewSecondary(int secondary) { return new IntegerRange(Primary, secondary); }

		/// <summary>
		/// Takes a value between 0.0 and 1.0 and returns where that value would
		/// fall in a range between the Primary and Secondary values.
		/// </summary>
		/// <returns>The evaluate.</returns>
		/// <param name="normal">Normal.</param>
		public int Evaluate(float normal) { return Primary + Mathf.RoundToInt(Delta * normal); }

		/// <summary>
		/// Takes a value that should be normalized, but may not be between 0.0
		/// and 1.0, and clamps it to a normal range before evaluating it
		/// between the Primary and Secondary values.
		/// </summary>
		/// <returns>The clamped.</returns>
		/// <param name="normal">Normal.</param>
		public int EvaluateClamped(float normal) { return Evaluate(Mathf.Clamp01(normal)); }

		/// <summary>
		/// Takes a value and finds its unclamped normal between the Primary and
		/// Secondary values. May not return a value between 0.0 and 1.0.
		/// </summary>
		/// <returns>The progress.</returns>
		/// <param name="value">Value.</param>
		public float Progress(int value)
		{
			if (0 == Delta) return 1f;
			return (value - (float)primary) / Delta;
		}

		/// <summary>
		/// Takes a value and finds its clamped normal between the Primary and
		/// Secondary values. Will always return a value between 0.0 and 1.0.
		/// </summary>
		/// <returns>The clamped.</returns>
		/// <param name="value">Value.</param>
		public float ProgressClamped(int value)
		{
			return Progress(
				Mathf.Clamp(
					value,
					Mathf.Min(Primary, Secondary),
					Mathf.Max(Primary, Secondary)
				)
			);
		}
		
		public static implicit operator IntegerRange(Vector2 v)
		{
			return new IntegerRange(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));
		}
		
		public static implicit operator Vector2(IntegerRange f)
		{
			return new Vector2(f.Primary, f.Secondary);
		}
		
		public static implicit operator Vector2Int(IntegerRange f)
		{
			return new Vector2Int(f.Primary, f.Secondary);
		}
	}
}