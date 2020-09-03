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
			static PropertyKeyValue[] Combine(
				string type,
				PropertyKeyValue[] required,
				params PropertyKeyValue[] overrides
			)
			{
				var consolidated = new Dictionary<string, PropertyKeyValue>();

				consolidated.Add(Keys.Shared.Type.Key, Keys.Shared.Type.Pair(type));
				
				foreach (var e in required.Concat(overrides)) consolidated[e.Key] = e;

				return consolidated.Values.ToArray();
			}
			
			public static class Resource
			{
				static PropertyKeyValue[] Create(
					string id,
					params PropertyKeyValue[] overrides
				)
				{
					const float DefaultDecayRate = 1f; // Per real second at 1x speed
					const float DefaultDecayMaximum = DayTime.RealTimeToSimulationTime * DefaultDecayRate * (60f * 10f); // Real seconds to decay

					return Combine(
						Values.Shared.Types.Resource,
						new[]
						{
							Keys.Resource.Id.Pair(id),

							Keys.Resource.Logistics.State.Pair(Values.Resource.Logistics.States.None),
							Keys.Resource.Logistics.Promised.Pair(),

							Keys.Resource.Decay.IsEnabled.Pair(true),
							Keys.Resource.Decay.Maximum.Pair(DefaultDecayMaximum),
							Keys.Resource.Decay.Current.Pair(DefaultDecayMaximum),
							Keys.Resource.Decay.Previous.Pair(DefaultDecayMaximum),
							Keys.Resource.Decay.Rate.Pair(DefaultDecayRate),
							Keys.Resource.Decay.RatePredicted.Pair(DefaultDecayRate)
						},
						overrides
					);
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
					params PropertyKeyValue[] overrides
				)
				{
					return Combine(
						Values.Shared.Types.Capacity,
						new[]
						{
							Keys.Capacity.ResourceId.Pair(resourceId),
							Keys.Capacity.Desire.Pair(Values.Capacity.Desires.NotCalculated),
							Keys.Capacity.TimeoutExpired.Pair(),
							Keys.Capacity.CurrentCount.Pair(),
							Keys.Capacity.MaximumCount.Pair(count),
							Keys.Capacity.TargetCount.Pair(count)
						},
						overrides
					);
				}
			}
			
			public static class Reservation
			{
				public static PropertyKeyValue[] OfInput(
					string resourceId,
					long capacityId,
					params PropertyKeyValue[] overrides
				)
				{
					return Of(
						resourceId,
						capacityId,
						Values.Reservation.States.Input,
						overrides
					);
				}
				
				public static PropertyKeyValue[] OfOutput(
					string resourceId,
					long capacityId,
					params PropertyKeyValue[] overrides
				)
				{
					return Of(
						resourceId,
						capacityId,
						Values.Reservation.States.Output,
						overrides
					);
				}
				
				static PropertyKeyValue[] Of(
					string resourceId,
					long capacityId,
					string state,
					params PropertyKeyValue[] overrides
				)
				{
					return Combine(
						Values.Shared.Types.Reservation,
						new []
						{
							Keys.Reservation.ResourceId.Pair(resourceId),
							Keys.Reservation.CapacityId.Pair(capacityId),
							Keys.Reservation.State.Pair(state),
							Keys.Reservation.IsPromised.Pair()
						},
						overrides
					);
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
					public static readonly PropertyKey<bool> Promised = Create<bool>(nameof(Promised));
				}
			
				public static class Decay
				{
					static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Resource), nameof(Decay), suffix);
				
					public static readonly PropertyKey<bool> IsEnabled = Create<bool>(nameof(IsEnabled));
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
				public static readonly PropertyKey<string> Desire = Create<string>(nameof(Desire));
				public static readonly PropertyKey<long> TimeoutExpired = Create<long>(nameof(TimeoutExpired));
				public static readonly PropertyKey<int> CurrentCount = Create<int>(nameof(CurrentCount));
				public static readonly PropertyKey<int> MaximumCount = Create<int>(nameof(MaximumCount));
				public static readonly PropertyKey<int> TargetCount = Create<int>(nameof(TargetCount));
			}
			
			public static class Reservation
			{
				static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Reservation), suffix);
				
				public static readonly PropertyKey<string> ResourceId = Create<string>(nameof(ResourceId));
				public static readonly PropertyKey<long> CapacityId = Create<long>(nameof(CapacityId));
				public static readonly PropertyKey<string> State = Create<string>(nameof(State));
				public static readonly PropertyKey<bool> IsPromised = Create<bool>(nameof(IsPromised));
			}
		}

		public static class Values
		{
			public static class Shared
			{
				public static class Types
				{
					public static readonly string Resource = nameof(Resource).ToSnakeCase();
					public static readonly string Capacity = nameof(Capacity).ToSnakeCase();
					public static readonly string Reservation = nameof(Reservation).ToSnakeCase();
				}
			}
			
			public static class Resource
			{
				public static class Ids
				{
					public static readonly string Stalk = nameof(Stalk).ToSnakeCase();
				}
				
				public static class Logistics
				{
					public static class States
					{
						public static readonly string None = nameof(None).ToSnakeCase();
						public static readonly string Input = nameof(Input).ToSnakeCase();
						public static readonly string Output = nameof(Output).ToSnakeCase();
						
						// I don't think I need these below
						// public static readonly string Distribute = nameof(Distribute).ToSnakeCase();
						// public static readonly string Transit = nameof(Transit).ToSnakeCase();
						// public static readonly string Forbidden = nameof(Forbidden).ToSnakeCase();
					}
				}
			}
			
			public static class Capacity
			{
				public static class Desires
				{
					public static readonly string None = nameof(None).ToSnakeCase();
					public static readonly string NotCalculated = nameof(NotCalculated).ToSnakeCase();
					public static readonly string Distribute = nameof(Distribute).ToSnakeCase();
					public static readonly string Fulfill = nameof(Fulfill).ToSnakeCase();
				}
			}
			
			public static class Reservation
			{
				public static class States
				{
					public static readonly string Input = nameof(Input).ToSnakeCase();
					public static readonly string Output = nameof(Output).ToSnakeCase();
				}
			}
		}
	}
}