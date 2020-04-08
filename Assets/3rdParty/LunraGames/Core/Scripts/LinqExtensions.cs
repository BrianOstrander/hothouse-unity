using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using UnityEngine;

using LunraGames.NumberDemon;

namespace LunraGames
{
	public static class LinqExtensions
	{
		public static IEnumerable<string> FriendlyMatch(this IEnumerable<string> entries, string search)
		{
			return FriendlyMatch(entries, search, s => s);
		}

		public static IEnumerable<T> FriendlyMatch<T>(this IEnumerable<T> entries, string search, Func<T, string> keySelector)
		{
			if (string.IsNullOrEmpty(search)) return entries;
			var pattern = string.Empty;
			foreach (var character in search) pattern += character+".*";
			var regex = new Regex(pattern, RegexOptions.IgnoreCase);
			return entries.Where(e => regex.IsMatch(keySelector(e)));
		}

		public static T FirstOrFallback<T>(this IEnumerable<T> entries, Func<T, bool> predicate, T fallback = default(T))
		{
			return entries.DefaultIfEmpty(fallback).FirstOrDefault(predicate);
		}

		public static T FirstOrFallback<T>(this IEnumerable<T> entries, T fallback = default(T))
		{
			return entries.DefaultIfEmpty(fallback).FirstOrDefault();
		}

		public static T LastOrFallback<T>(this IEnumerable<T> entries, Func<T, bool> predicate, T fallback = default(T))
		{
			return entries.DefaultIfEmpty(fallback).LastOrDefault(predicate);
		}

		public static T LastOrFallback<T>(this IEnumerable<T> entries, T fallback = default(T))
		{
			return entries.DefaultIfEmpty(fallback).LastOrDefault();
		}

		public static T Random<T>(this IEnumerable<T> entries, T fallback = default(T))
		{
			if (entries == null || entries.Count() == 0) return fallback;
			return entries.ElementAt(UnityEngine.Random.Range(0, entries.Count()));
		}

		public static T RandomWeighted<T>(this IEnumerable<T> entries, Func<T, float> getWeight, T fallback = default(T))
		{
			var ordered = entries.OrderBy(getWeight);
			var keyed = new List<KeyValuePair<float, T>>();
			var offset = 0f;
			foreach (var entry in ordered)
			{
				offset += getWeight(entry);
				keyed.Add(new KeyValuePair<float, T>(offset, entry));
			}
			var selectedOffset = DemonUtility.GetNextFloat(max: offset);

			var lastOffset = 0f;
			foreach (var entry in keyed)
			{
				if ((Mathf.Approximately(entry.Key, lastOffset) || lastOffset < selectedOffset) && selectedOffset < entry.Key)
				{
					return entry.Value;
				}
			}
			return keyed.Last().Value;
		}

#if !CSHARP_7_3_OR_NEWER

		public static IEnumerable<T> Append<T>(this IEnumerable<T> entries, T element)
		{
			if (entries == null) throw new ArgumentNullException("entries");
			return ConcatIterator(entries, element, false);
		}

		public static IEnumerable<T> Prepend<T>(this IEnumerable<T> entries, T element)
		{
			if (entries == null) throw new ArgumentNullException("entries");
			return ConcatIterator(entries, element, true);
		}

#endif

		static IEnumerable<T> ConcatIterator<T>(IEnumerable<T> entries, T element, bool start)
		{
			if (start) yield return element;
			foreach (var entry in entries) yield return entry;
			if (!start) yield return element;
		}

		public static bool ContainsOrIsEmpty<T>(this IEnumerable<T> entries, T element)
		{
			if (entries.Count() == 0) return true;
			return entries.Contains(element);
		}

		/// <summary>
		/// Checks if all elements exist in both collections, and no additional
		/// elements are present.
		/// </summary>
		/// <returns><c>true</c>, if all elements of one collection are present in the second, <c>false</c> otherwise.</returns>
		/// <param name="entries">Entries.</param>
		/// <param name="second">Second.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static bool IntersectEqual<T>(this IEnumerable<T> entries, IEnumerable<T> second)
		{
			var entriesCount = entries.Count();
			if (entriesCount != second.Count()) return false;
			return entries.Intersect(second).Count() == entriesCount;
		}

		public static bool None<T>(this IEnumerable<T> entries)
		{
			return !entries.Any();
		}

		public static bool None<T>(this IEnumerable<T> entries, Func<T, bool> predicate)
		{
			return !entries.Any(predicate);
		}

		public static IEnumerable<T> ExceptOne<T>(this IEnumerable<T> entries, T element)
		{
			return entries.Except(new T[] { element });
		}
	}
}