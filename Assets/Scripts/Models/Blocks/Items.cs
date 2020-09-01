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
					const float DefaultDecayMaximum = DayTime.RealTimeToSimulationTime * DefaultDecayRate * (60f * 10f); // Real seconds to decay
				
					var result = new []
					{
						Keys.Shared.Type.Pair(Values.Shared.Types.Resource),
						
						Keys.Resource.Id.Pair(id),
					
						Keys.Resource.Logistics.State.Pair(Values.Resource.Logistics.States.None),
					
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

				public static readonly PropertyKeyValue[] Stalk = Create(Values.Resource.Ids.Stalk);
			}
			
			public static class Capacity
			{
				public static PropertyKeyValue[] OfZero(
					string resourceId,
					params PropertyKeyValue[] keyValues
				)
				{
					return Of(resourceId, 0, keyValues);
				}
				public static PropertyKeyValue[] Of(
					string resourceId,
					int count,
					params PropertyKeyValue[] keyValues
				)
				{
					var result = new []
					{
						Keys.Shared.Type.Pair(Values.Shared.Types.Capacity),
						
						Keys.Capacity.ResourceId.Pair(resourceId),
						Keys.Capacity.Maximum.Pair(count),
						Keys.Capacity.Desired.Pair(count),
						Keys.Capacity.Fulfilled.Pair()
					};

					var consolidated = new Dictionary<string, PropertyKeyValue>();

					foreach (var entry in result.Concat(keyValues)) consolidated[entry.Key] = entry;

					return consolidated.Values.ToArray();
				}
			}
		}

		public static class Keys
		{
			static PropertyKey<T> CreateKey<T>(params string[] elements) => new PropertyKey<T>(elements.ToPunctualSnakeCase());

			public static class Shared
			{
				static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Shared), suffix);
				
				public static readonly PropertyKey<string> Type = Create<string>(nameof(Type));
			}
			
			public static class Resource
			{
				static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Resource), suffix);

				public static readonly PropertyKey<string> Id = Create<string>(nameof(Id));

				public static class Logistics
				{
					static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Resource), nameof(Logistics), suffix);
				
					public static readonly PropertyKey<string> State = Create<string>(nameof(State));
				}
			
				public static class Decay
				{
					static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Resource), nameof(Decay), suffix);
				
					public static readonly PropertyKey<bool> Enabled = Create<bool>(nameof(Enabled));
					public static readonly PropertyKey<float> Maximum = Create<float>(nameof(Maximum));
					public static readonly PropertyKey<float> Current = Create<float>(nameof(Current));
					public static readonly PropertyKey<float> Previous = Create<float>(nameof(Previous));
					public static readonly PropertyKey<float> Rate = Create<float>(nameof(Rate));
					public static readonly PropertyKey<float> RatePredicted = Create<float>(nameof(RatePredicted));
				}
			}

			public static class Capacity
			{
				static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Capacity), suffix);
				
				public static readonly PropertyKey<string> ResourceId = Create<string>(nameof(ResourceId));
				public static readonly PropertyKey<int> Maximum = Create<int>(nameof(Maximum));
				public static readonly PropertyKey<int> Desired = Create<int>(nameof(Desired));
				public static readonly PropertyKey<int> Fulfilled = Create<int>(nameof(Fulfilled));
			}
		}

		public static class Values
		{
			public static class Shared
			{
				public static class Types
				{
					static string Create(string type) => type.ToSnakeCase();
					
					public static readonly string Resource = Create(nameof(Resource));
					public static readonly string Capacity = Create(nameof(Capacity));
				}
			}
			
			public static class Resource
			{
				public static class Ids
				{
					static string Create(string id) => id.ToSnakeCase();

					public static readonly string Stalk = Create(nameof(Stalk));
				}
				
				public static class Logistics
				{
					public static class States
					{
						public static readonly string None = nameof(None).ToSnakeCase();
						public static readonly string Distribute = nameof(Distribute).ToSnakeCase();
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