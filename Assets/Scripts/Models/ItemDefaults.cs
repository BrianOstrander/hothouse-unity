using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Satchel;

namespace Lunra.Hothouse.Models
{
	public static class ItemDefaults
	{
		public static class Resource
		{
			static PropertyKeyValue[] Create(
				string id,
				params PropertyKeyValue[] keyValues
			)
			{
				const float DefaultDecayRate = 1f; // Per real second at 1x speed
				const float DefaultDecayMaximum = DayTime.RealTimeToSimulationTime * DefaultDecayRate * (60f * 1f); // About a minute
				
				var result = new PropertyKeyValue[]
				{
					ItemKeys.Resource.Id.Pair(id.ToSnakeCase()),
					ItemKeys.Resource.InventoryId.Pair(),
					
					ItemKeys.Resource.Logistics.Type.Pair(ItemEnumerations.Resource.Logistics.Types.None),
					ItemKeys.Resource.Logistics.State.Pair(ItemEnumerations.Resource.Logistics.States.None),
					ItemKeys.Resource.Logistics.Available.Pair(-1),
					ItemKeys.Resource.Logistics.Promised.Pair(-1),
					
					ItemKeys.Resource.Decay.Enabled.Pair(true),
					ItemKeys.Resource.Decay.ForbidDestruction.Pair(),
					ItemKeys.Resource.Decay.Maximum.Pair(DefaultDecayMaximum),
					ItemKeys.Resource.Decay.Current.Pair(DefaultDecayMaximum),
					ItemKeys.Resource.Decay.Previous.Pair(DefaultDecayMaximum),
					ItemKeys.Resource.Decay.Rate.Pair(DefaultDecayRate),
					ItemKeys.Resource.Decay.RatePredicted.Pair(DefaultDecayRate)
				};

				var consolidated = new Dictionary<string, PropertyKeyValue>();

				foreach (var entry in result.Concat(keyValues)) consolidated[entry.Key] = entry;

				return consolidated.Values.ToArray();
			}

			public static readonly PropertyKeyValue[] Stalk = Create(nameof(Stalk));
		}
	}
}