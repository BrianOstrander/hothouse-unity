using System;
using System.Linq;
using Lunra.Core;
using Newtonsoft.Json;

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
					.Select(f => new Entry(f))
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

		public struct Entry
		{
			[JsonProperty] public ItemFilter Filter { get; private set; }
			[JsonProperty] public int CountLimit { get; private set; }

			public Entry(
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

		[JsonProperty] public int CountLimit { get; private set; }
		[JsonProperty] public int DefaultCountLimit { get; private set; }
		[JsonProperty] public Entry[] Entries { get; private set; }
		[JsonProperty]  public bool IsIgnored { get; private set; } 
		
		public ItemConstraint(
			int countLimit,
			int defaultCountLimit,
			params Entry[] entries
		)
		{
			if (countLimit < 0) throw new ArgumentOutOfRangeException(nameof(countLimit), "Must be greater than or equal to zero");
			if (defaultCountLimit < 0) throw new ArgumentOutOfRangeException(nameof(defaultCountLimit), "Must be greater than or equal to zero");
			
			CountLimit = countLimit;
			DefaultCountLimit = defaultCountLimit;
			Entries = entries;

			IsIgnored = CountLimit == int.MaxValue && DefaultCountLimit == int.MaxValue && Entries.None();
		}

		public override string ToString()
		{
			var result = $"\nTotal Count Limit: {CountLimit}";
			result += $"\nDefault Count Limit: {DefaultCountLimit}";
			result += "\nEntries: ";

			if (Entries.None()) result += "None";
			else
			{
				for (var i = 0; i < Entries.Length; i++)
				{
					result += $"\n---- [ {i} ] --------------------";
					result += $"\n{Entries[i]}";
				}
			}
			
			return result;
		}
	}
}