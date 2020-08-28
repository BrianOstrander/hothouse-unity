using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Satchel;

namespace Lunra.Hothouse.Models
{
	public static class Items // rename to utility or something
	{
		public static class Instantiate
		{
			public static class Resource
			{
				static PropertyKeyValue[] Create(
					string id,
					params PropertyKeyValue[] keyValues
				)
				{
					const float DefaultDecayRate = 1f; // Per real second at 1x speed
					const float DefaultDecayMaximum = DayTime.RealTimeToSimulationTime * DefaultDecayRate * (5f); // Real seconds to decay
				
					var result = new []
					{
						Keys.Resource.Id.Pair(id.ToSnakeCase()),
						Keys.Resource.InventoryId.Pair(),
					
						Keys.Resource.Logistics.Type.Pair(Values.Resource.Logistics.Types.None),
						Keys.Resource.Logistics.State.Pair(Values.Resource.Logistics.States.None),
						Keys.Resource.Logistics.Available.Pair(-1),
						Keys.Resource.Logistics.Promised.Pair(-1),
					
						Keys.Resource.Decay.Enabled.Pair(true),
						Keys.Resource.Decay.Maximum.Pair(DefaultDecayMaximum),
						Keys.Resource.Decay.Current.Pair(DefaultDecayMaximum),
						Keys.Resource.Decay.Previous.Pair(DefaultDecayMaximum),
						Keys.Resource.Decay.Rate.Pair(DefaultDecayRate),
						Keys.Resource.Decay.RatePredicted.Pair(DefaultDecayRate)
					};

					var consolidated = new Dictionary<string, PropertyKeyValue>();

					foreach (var entry in result.Concat(keyValues)) consolidated[entry.Key] = entry;

					return consolidated.Values.ToArray();
				}

				public static readonly PropertyKeyValue[] Stalk = Create(nameof(Stalk));
			}
		}

		public static class Keys
		{
			static PropertyKey<T> CreateKey<T>(params string[] elements) => new PropertyKey<T>(elements.ToPunctualSnakeCase());
		
			public static class Resource
			{
				static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Keys), suffix);

				public static readonly PropertyKey<string> Id = Create<string>(nameof(Id));
				public static readonly PropertyKey<string> InventoryId = Create<string>(nameof(InventoryId));

				public static class Logistics
				{
					static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Keys), nameof(Logistics), suffix);
				
					public static readonly PropertyKey<string> Type = Create<string>(nameof(Type));
					public static readonly PropertyKey<string> State = Create<string>(nameof(State));
					public static readonly PropertyKey<int> Available = Create<int>(nameof(Available));
					public static readonly PropertyKey<int> Promised = Create<int>(nameof(Promised));
				}
			
				public static class Decay
				{
					static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Keys), nameof(Decay), suffix);
				
					public static readonly PropertyKey<bool> Enabled = Create<bool>(nameof(Enabled));
					public static readonly PropertyKey<float> Maximum = Create<float>(nameof(Maximum));
					public static readonly PropertyKey<float> Current = Create<float>(nameof(Current));
					public static readonly PropertyKey<float> Previous = Create<float>(nameof(Previous));
					public static readonly PropertyKey<float> Rate = Create<float>(nameof(Rate));
					public static readonly PropertyKey<float> RatePredicted = Create<float>(nameof(RatePredicted));
				}
			}	
		}

		public static class Values
		{
			public static class Resource
			{
				public static class Logistics
				{
					public static class Types
					{
						public static readonly string None = nameof(None).ToSnakeCase();
						public static readonly string Construction = nameof(Construction).ToSnakeCase();
						public static readonly string Recipe = nameof(Recipe).ToSnakeCase();
						public static readonly string Goal = nameof(Goal).ToSnakeCase();
					}
			
					public static class States
					{
						public static readonly string None = nameof(None).ToSnakeCase();
						public static readonly string Distribute = nameof(Distribute).ToSnakeCase();
						public static readonly string Desire = nameof(Desire).ToSnakeCase();
						public static readonly string Input = nameof(Input).ToSnakeCase();
						public static readonly string Output = nameof(Output).ToSnakeCase();
						public static readonly string Transit = nameof(Transit).ToSnakeCase();
						public static readonly string Forbidden = nameof(Forbidden).ToSnakeCase();
					}
				}
			}
		}
	}
}