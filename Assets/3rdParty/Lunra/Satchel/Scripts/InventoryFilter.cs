using System;
using Newtonsoft.Json;

namespace Lunra.Satchel
{
	public struct InventoryFilter
	{
		[JsonProperty] public PropertyFilter Filter { get; private set; }
		[JsonProperty] public int CountLimit { get; private set; }

		public InventoryFilter(
			PropertyFilter filter,
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
}