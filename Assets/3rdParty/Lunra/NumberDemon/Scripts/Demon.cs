using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using UnityEngine;

using Random = System.Random;

namespace Lunra.NumberDemon
{
	public class Demon 
	{
		static Random seedGenerator = new Random();
		
		Random generator;
		public int Seed { get; private set; }

		public Demon() : this(seedGenerator.Next()) {}
		public Demon(int seed)
		{
			Seed = seed;
			generator = new Random(seed);
		}

		#region Properties
		public bool NextBool => generator.Next(2) == 0;
		public int NextInteger => generator.Next();
		public long NextLong => BitConverter.ToInt64(GetNextBytes(8), 0);

		/// <summary>
		/// Gets the next float value between the inclusive minimum 0.0f and the exclusive maximum 1.0f.
		/// </summary>
		/// <value>The next float.</value>
		public float NextFloat => (float)generator.NextDouble();

		public Color NextColor => new Color(NextFloat, NextFloat, NextFloat);

		public Vector3 NextNormal =>
			new Vector3(
				GetNextFloat(-1f, 1f),
				GetNextFloat(-1f, 1f),
				GetNextFloat(-1f, 1f)
			).normalized;
		#endregion

		#region Methods
		public byte[] GetNextBytes(int count)
		{
			var bytes = new byte[count];
			generator.NextBytes(bytes);
			return bytes;
		}

		/// <summary>
		/// Gets the next integer between the inclusive minimum and exclusive maximum.
		/// </summary>
		/// <returns>The next integer.</returns>
		/// <param name="min">Min, included.</param>
		/// <param name="max">Max, excluded.</param>
		public int GetNextInteger(int min = 0, int max = int.MaxValue) { return generator.Next(min, max); }

		/// <summary>
		/// Gets the next float between the inclusive minimum and exclusive maximum.
		/// </summary>
		/// <returns>The next float.</returns>
		/// <param name="min">Min, included.</param>
		/// <param name="max">Max, excluded.</param>
		public float GetNextFloat(float min = 0f, float max = float.MaxValue) 
		{
			if (max < min) throw new ArgumentOutOfRangeException(nameof(max), "Value max must be larger than min.");
			if (Mathf.Approximately(min, max)) return min;
			var delta = max - min;
			return min + (NextFloat * delta);
		}

		public T GetNextFrom<T>(
			IEnumerable<T> entries,
			T fallback = default
		)
		{
			if (entries == null || entries.None()) return fallback;
			return entries.ElementAt(GetNextInteger(0, entries.Count()));
		}
		
		public Quaternion GetNextRotation() => Quaternion.AngleAxis(GetNextFloat(0f, 360f), Vector3.up);
		#endregion

	}
}