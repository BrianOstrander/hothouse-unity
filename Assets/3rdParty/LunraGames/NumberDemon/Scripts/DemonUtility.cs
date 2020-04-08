using System;
using System.Linq;

using UnityEngine;

namespace LunraGames.NumberDemon
{
	public static class DemonUtility
	{
		static Demon Generator = new Demon();

		const int IntHalfValue = (int.MaxValue / 2) + 1;
		const uint UintHalfValue = (uint.MaxValue / 2u) + 1u;
		const ulong UlongHalfValue = (ulong.MaxValue / 2uL) + 1uL;

		#region CantorPairs
		/// <summary>
		/// Cantor pairs the provided values. If you have a large seed value, 
		/// supply it last since that seems to reduce duplicates.
		/// </summary>
		/// <remarks>
		/// Given a unique pair of integers and their order, return a unique 
		/// integer. When pairing two integers and at least one value is above 
		/// 1,073,741,824, they roll over. They roll under somewhere too, but 
		/// I'm too lazy to check.
		/// </remarks>
		/// <returns>The unique seed of these values.</returns>
		/// <param name="values">Values.</param>
		public static int CantorPairs(params int[] values)
		{
			var uValues = new uint[values.Length];
			for (var i = 0; i < values.Length; i++) uValues[i] = ToUint(values[i]);
			return ToInt(CantorPairs(uValues));
		}

		/// <summary>
		/// Cantor pairs the provided values. If you have a large seed value, 
		/// supply it last since that seems to reduce duplicates.
		/// </summary>
		/// <returns>The unique seed of these values.</returns>
		/// <param name="values">Values.</param>
		public static uint CantorPairs(params uint[] values)
		{
			if (values.Length == 0) return 0;
			if (values.Length == 1) return values.First();

			var last = values.Last();
			values = values.Take(values.Length - 1).ToArray();

			return ((CantorPairs(values) + last) * (CantorPairs(values) + last + 1) / 2) + last;
		}

		/// <summary>
		/// Cantor pairs the provided values. If you have a large seed value, 
		/// supply it last since that seems to reduce duplicates.
		/// </summary>
		/// <returns>The unique seed of these values.</returns>
		/// <param name="values">Values.</param>
		public static long CantorPairs(params long[] values)
		{
			var uValues = new ulong[values.Length];
			for (var i = 0; i < values.Length; i++) uValues[i] = ToUlong(values[i]);
			return ToLong(CantorPairs(uValues));
		}

		/// <summary>
		/// Cantor pairs the provided values. If you have a large seed value, 
		/// supply it last since that seems to reduce duplicates.
		/// </summary>
		/// <returns>The unique seed of these values.</returns>
		/// <param name="values">Values.</param>
		public static ulong CantorPairs(params ulong[] values)
		{
			if (values.Length == 0) return 0;
			if (values.Length == 1) return values.First();

			var last = values.Last();
			values = values.Take(values.Length - 1).ToArray();

			return ((CantorPairs(values) + last) * (CantorPairs(values) + last + 1uL) / 2uL) + last;
		}

		#endregion

		/// <summary>
		/// Takes an integer and maps it to an unsigned integer, where int.MinValue == 0u.
		/// </summary>
		/// <returns>The uint.</returns>
		/// <param name="value">Value.</param>
		public static uint ToUint(int value)
		{
			if (value < 0) return UintHalfValue - ((uint)Math.Abs((long)value));
			return UintHalfValue + (uint)value;
		}

		/// <summary>
		/// Takes an unsigned integer and maps it to an integer, where int.MinValue == 0u.
		/// </summary>
		/// <returns>The int.</returns>
		/// <param name="value">Value.</param>
		public static int ToInt(uint value)
		{
			if (value < UintHalfValue) return 0 - (int)(UintHalfValue - value);
			return (int)(value - UintHalfValue);
		}

		/// <summary>
		/// Takes a long and maps it to an unsigned long, where long.MinValue = 0uL.
		/// </summary>
		/// <returns>The ulong.</returns>
		/// <param name="value">Value.</param>
		public static ulong ToUlong(long value)
		{
			if (value < 0L) 
			{
				if (value == long.MinValue) return 0uL;
				return UlongHalfValue - ((ulong)Math.Abs(value));
			}
			return UlongHalfValue + (ulong)value;
		}

		/// <summary>
		/// Takes an unsigned long and maps it to a long, where long.MinValue = 0uL.
		/// </summary>
		/// <returns>The long.</returns>
		/// <param name="value">Value.</param>
		public static long ToLong(ulong value)
		{
			if (value < UlongHalfValue) return 0L - (long)(UlongHalfValue - value);
			return (long)(value - UlongHalfValue);
		}

		public static bool NextBool => Generator.NextBool;
		public static int NextInteger => Generator.NextInteger;
		public static long NextLong => Generator.NextLong;

		/// <summary>
		/// Gets the next float value between the inclusive minimum 0.0f and the exclusive maximum 1.0f.
		/// </summary>
		/// <value>The next float.</value>
		public static float NextFloat => Generator.NextFloat;

		public static Color NextColor => new Color(NextFloat, NextFloat, NextFloat);

		public static byte[] GetNextBytes(int count) { return Generator.GetNextBytes(count); }

		/// <summary>
		/// Gets the next integer between the inclusive minimum and exclusive maximum.
		/// </summary>
		/// <returns>The next integer.</returns>
		/// <param name="min">Min, included.</param>
		/// <param name="max">Max, excluded.</param>
		public static int GetNextInteger(int min = 0, int max = int.MaxValue) { return Generator.GetNextInteger(min, max); }

		/// <summary>
		/// Gets the next float between the inclusive minimum and exclusive maximum.
		/// </summary>
		/// <returns>The next float.</returns>
		/// <param name="min">Min, included.</param>
		/// <param name="max">Max, excluded.</param>
		public static float GetNextFloat(float min = 0f, float max = float.MaxValue) { return Generator.GetNextFloat(min, max); }
	}
}