using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Satchel
{
	public class ItemConstraint
	{
		public static ItemConstraint Ignored() => new ItemConstraint(int.MaxValue, int.MaxValue);

		public static ItemConstraint ByFilter(
			params ItemFilter[] filters
		)
		{
			return ByFilter(
				int.MaxValue,
				filters
			);
		}
		
		public static ItemConstraint ByFilter(
			int countLimit,
			params ItemFilter[] filters
		)
		{
			return new ItemConstraint(
				countLimit,
				0,
				filters
					.Select(f => new Restriction(f))
					.ToArray()
			);
		}
		
		public static ItemConstraint ByCount(
			int countLimit
		)
		{
			return new ItemConstraint(
				countLimit,
				countLimit
			);
		}

		public struct Restriction
		{
			[JsonProperty] public ItemFilter Filter { get; private set; }
			[JsonProperty] public int CountLimit { get; private set; }

			public Restriction(
				ItemFilter filter,
				int countLimit = int.MaxValue
			)
			{
				Filter = filter ?? throw new ArgumentNullException(nameof(filter));
				
				if (countLimit < 0) throw new ArgumentOutOfRangeException(nameof(countLimit), "Must be greater than or equal to zero");
				
				CountLimit = countLimit;
			}

			public override string ToString()
			{
				var result = $"Count Limit: {CountLimit}";
				result += "\n" + Filter.ToStringVerbose();

				return result;
			}
		}

		[JsonProperty] public int Limit { get; private set; }
		[JsonProperty] public int LimitDefault { get; private set; }
		[JsonProperty] public Restriction[] Restrictions { get; private set; }
		[JsonProperty]  public bool IsIgnored { get; private set; }

		bool isInitialized;
		ItemStore itemStore;
		
		public ItemConstraint(
			int limit,
			int limitDefault,
			params Restriction[] restrictions
		)
		{
			if (limit < 0) throw new ArgumentOutOfRangeException(nameof(limit), "Must be greater than or equal to zero");
			if (limitDefault < 0) throw new ArgumentOutOfRangeException(nameof(limitDefault), "Must be greater than or equal to zero");
			
			Limit = limit;
			LimitDefault = limitDefault;
			Restrictions = restrictions;

			IsIgnored = Limit == int.MaxValue && LimitDefault == int.MaxValue && Restrictions.None();
		}
		
		public ItemConstraint Initialize(ItemStore itemStore)
		{
			this.itemStore = itemStore ?? throw new ArgumentNullException(nameof(itemStore));

			if (isInitialized) return this;
			
			isInitialized = true;
			
			foreach (var entry in Restrictions) entry.Filter.Initialize(itemStore);

			return this;
		}

		/// <summary>
		/// Applies this constraint to an arbitrary collection of stacks.
		/// </summary>
		/// <remarks>
		/// Stacks do not need to be in a unique list, but early indexes will be prioritized for inclusion over later
		/// indexes.
		/// </remarks>
		/// <param name="stacks">Duplicate tolerant collection of stacks ordered by inclusion priority.</param>
		/// <param name="result">Result of constraint application.</param>
		/// <param name="overflow">Overflow from constraint application.</param>
		/// <returns><c>true</c> if overflow occured, <c>false</c> otherwise.</returns>
		public bool Apply(
			ItemStack[] stacks,
			out ItemStack[] result,
			out ItemStack[] overflow
		)
		{
			var sorted = new Dictionary<ulong, (Item Item, int Count, int Overflow, int Limit)>();

			var countTotal = 0;
			var anyOverflow = false;
			
			foreach (var stack in stacks)
			{
				if (stack.Count == 0) continue;

				var countBudget = Mathf.Max(0, Limit - countTotal);
				var isFilled = countBudget == 0;

				if (sorted.TryGetValue(stack.Id, out var sortedEntry))
				{
					if (isFilled || sortedEntry.Limit <= sortedEntry.Count)
					{
						sortedEntry.Overflow += stack.Count;
						countTotal += stack.Count;
						anyOverflow = true;
					}
					else
					{
						var stackBudget = Mathf.Max(0, sortedEntry.Limit - sortedEntry.Count);
						
						if (stack.Count <= stackBudget)
						{
							sortedEntry.Count += stack.Count;
							countTotal += stack.Count;
						}
						else
						{
							sortedEntry.Count += stackBudget;
							sortedEntry.Overflow += stack.Count - stackBudget;
							countTotal += stackBudget;
							anyOverflow = true;
						}
					}
					
					sorted[stack.Id] = sortedEntry;
					continue;
				}
				
				if (!itemStore.TryGet(stack.Id, out var item))
				{
					sorted[stack.Id] = default;
					Debug.LogError($"Unable to find item with Id [ {stack.Id} ]");
					continue;
				}

				sortedEntry = (item, 0, 0, int.MaxValue);

				if (isFilled)
				{
					sortedEntry.Overflow += stack.Count;
					sorted[stack.Id] = sortedEntry;
					anyOverflow = true;
					continue;
				}

				int? countLimitMinimum = null;

				foreach (var entry in Restrictions)
				{
					if (entry.Filter.Validate(item))
					{
						countLimitMinimum = countLimitMinimum.HasValue ? Mathf.Min(countLimitMinimum.Value, entry.CountLimit) : entry.CountLimit;
					}
				}

				sortedEntry.Limit = countLimitMinimum ?? LimitDefault;

				sortedEntry.Count = Mathf.Min(stack.Count, Mathf.Min(sortedEntry.Limit, countBudget));
				sortedEntry.Overflow = stack.Count - sortedEntry.Count;

				countTotal += sortedEntry.Count;
				anyOverflow |= 0 < sortedEntry.Overflow;

				sorted[stack.Id] = sortedEntry;
			}

			result = sorted
				.Where(s => s.Value.Item != null && 0 < s.Value.Count)
				.Select(s => new ItemStack(s.Key, s.Value.Count))
				.ToArray();

			if (anyOverflow)
			{
				overflow = sorted
					.Where(s => s.Value.Item != null && 0 < s.Value.Overflow)
					.Select(s => new ItemStack(s.Key, s.Value.Overflow))
					.ToArray();
			}
			else overflow = new ItemStack[0];

			return anyOverflow;
		}

		public override string ToString()
		{
			var result = $"\nTotal Count Limit: {Limit}";
			result += $"\nDefault Count Limit: {LimitDefault}";
			result += "\nEntries: ";

			if (Restrictions.None()) result += "None";
			else
			{
				for (var i = 0; i < Restrictions.Length; i++)
				{
					result += $"\n---- [ {i} ] --------------------";
					result += $"\n{Restrictions[i]}";
				}
			}
			
			return result;
		}
	}
}