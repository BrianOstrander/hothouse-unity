using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

using UnityEngine;

using Lunra.NumberDemon;

namespace Lunra.Core
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
			foreach (var character in search) pattern += character + ".*";
			var regex = new Regex(pattern, RegexOptions.IgnoreCase);
			return entries.Where(e => regex.IsMatch(keySelector(e)));
		}

		public static T FirstOrFallback<T>(this IEnumerable<T> entries, Func<T, bool> predicate, T fallback = default)
		{
			try
			{
				return entries.First(predicate);
			}
			catch (InvalidOperationException)
			{
				return fallback;
			}
		}

		public static T FirstOrFallback<T>(this IEnumerable<T> entries, T fallback = default)
		{
			return entries.DefaultIfEmpty(fallback).First();
		}

		public static T LastOrFallback<T>(this IEnumerable<T> entries, Func<T, bool> predicate, T fallback = default)
		{
			try
			{
				return entries.Last(predicate);
			}
			catch (InvalidOperationException)
			{
				return fallback;
			}
		}

		public static T LastOrFallback<T>(this IEnumerable<T> entries, T fallback = default)
		{
			return entries.DefaultIfEmpty(fallback).Last();
		}

		public static T Random<T>(this IEnumerable<T> entries, T fallback = default)
		{
			if (entries == null || entries.None()) return fallback;
			return entries.ElementAt(UnityEngine.Random.Range(0, entries.Count()));
		}

		public static T RandomWeighted<T>(this IEnumerable<T> entries, Func<T, float> getWeight, T fallback = default)
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

			return keyed.Any() ? keyed.Last().Value : fallback;
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
			if (entries.None()) return true;
			return entries.Contains(element);
		}

		/// <summary>
		/// Checks if all elements specified exist in the collection this is called on.
		/// </summary>
		/// <param name="entries"></param>
		/// <param name="second"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns>True if all elements are present.</returns>
		public static bool ContainsAll<T>(this IEnumerable<T> entries, IEnumerable<T> second)
		{
			return entries.Intersect(second).Count() == entries.Count();
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
			return entries.Except(new[] {element});
		}

		public static IEnumerable<T> Union<T>(this IEnumerable<T> source, T element)
		{
			return source.Union(Enumerable.Repeat(element, 1));
		}

		public static IEnumerable<T> DistinctBy<T, K>(
			this IEnumerable<T> elements,
			Func<T, K> keySelector
		)
		{
			return elements.GroupBy(keySelector)
				.Select(g => g.First());
		}

	public static ReadOnlyDictionary<TKey, TElement> ToReadonlyDictionary<TKey, TElement>(
			this Dictionary<TKey, TElement> source
		)
		{
			return new ReadOnlyDictionary<TKey, TElement>(source);	
		}
		
		public static ReadOnlyDictionary<TKey, TElement> ToReadonlyDictionary<TSource, TKey, TElement>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			Func<TSource, TElement> elementSelector
		)
		{
			return source.ToDictionary(
				keySelector,
				elementSelector
			).ToReadonlyDictionary();
		}
		
		public static Dictionary<TKey, TElement> ToDictionary<TKey, TElement>(
			this ReadOnlyDictionary<TKey, TElement> source
		)
		{
			return source.ToDictionary(
				kv => kv.Key,
				kv => source[kv.Key]
			);
		}
		
		public static IEnumerable<T> ToEnumerable<T>(this T element)
		{
			yield return element;
		}
		
		public static T[] WrapInArray<T>(this T element) => new [] { element };
		public static List<T> WrapInList<T>(this T element) => new List<T> { element };

		public static TValue TryGetValueOrFallback<TKey, TValue>(
			this IDictionary<TKey, TValue> source,
			TKey key,
			TValue fallback = default
		)
		{
			if (source.TryGetValue(key, out var value)) return value;
			return fallback;
		}
	}
}