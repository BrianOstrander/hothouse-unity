using Newtonsoft.Json;

namespace Lunra.Satchel
{
	public class ItemConstraint
	{
		public static ItemConstraint None() => new ItemConstraint(Types.None);
		
		public enum Types
		{
			None = 0,
			Forbidden = 10,
			Permitted = 20
		}

		public struct Entry
		{
			[JsonProperty] public ItemFilter Filter { get; private set; }
			[JsonProperty] public int CountLimit { get; private set; }
			[JsonProperty] public int WeightLimit { get; private set; }

			public Entry(
				ItemFilter filter,
				int countLimit,
				int weightLimit
			)
			{
				Filter = filter;
				CountLimit = countLimit;
				WeightLimit = weightLimit;
			}
		}

		[JsonProperty] public Types Type { get; private set; }
		[JsonProperty] public Entry[] Entries { get; private set; }

		public ItemConstraint(
			Types type,
			params Entry[] entries
		)
		{
			Type = type;
			Entries = entries;
		}
	}
}