using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Random = System.Random;

namespace LunraGames.NumberDemon
{
	public class Demon 
	{
		static Random SeedGenerator = new Random();
		Random Generator;

		public Demon() : this(SeedGenerator.Next()) {}
		public Demon(int seed) => Generator = new Random(seed);

		#region Properties
		public bool NextBool => Generator.Next(2) == 0;
		public int NextInteger => Generator.Next();
		public long NextLong => BitConverter.ToInt64(GetNextBytes(8), 0);

		/// <summary>
		/// Gets the next float value between the inclusive minimum 0.0f and the exclusive maximum 1.0f.
		/// </summary>
		/// <value>The next float.</value>
		public float NextFloat => (float)Generator.NextDouble();

		public Color NextColor => new Color(NextFloat, NextFloat, NextFloat);
		#endregion

		#region Methods
		public byte[] GetNextBytes(int count)
		{
			var bytes = new byte[count];
			Generator.NextBytes(bytes);
			return bytes;
		}

		/// <summary>
		/// Gets the next integer between the inclusive minimum and exclusive maximum.
		/// </summary>
		/// <returns>The next integer.</returns>
		/// <param name="min">Min, included.</param>
		/// <param name="max">Max, excluded.</param>
		public int GetNextInteger(int min = 0, int max = int.MaxValue) { return Generator.Next(min, max); }

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
		#endregion

	}
}