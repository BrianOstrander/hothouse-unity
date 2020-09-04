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

				consolidated.Add(Keys.Shared.Type.Key, (Keys.Shared.Type, type));
				
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
						new PropertyKeyValue[]
						{
							(Keys.Resource.Type, id),

							(Keys.Resource.Decay.IsEnabled, true),
							(Keys.Resource.Decay.Maximum, DefaultDecayMaximum),
							(Keys.Resource.Decay.Current, DefaultDecayMaximum),
							(Keys.Resource.Decay.Previous, DefaultDecayMaximum),
							(Keys.Resource.Decay.Rate, DefaultDecayRate),
							(Keys.Resource.Decay.RatePredicted, DefaultDecayRate)
						},
						overrides
					);
				}

				public static readonly PropertyKeyValue[] Stalk = Create(Values.Resource.Types.Stalk);
			}
			
			public static class Capacity
			{
				public static PropertyKeyValue[] CacheOfZero(
					string resourceType,
					params PropertyKeyValue[] overrides
				)
				{
					return Get(
						resourceType,
						0,
						true,
						overrides
					);
				}

				public static PropertyKeyValue[] CacheOf(
					string resourceType,
					int count,
					params PropertyKeyValue[] overrides
				)
				{
					return Get(
						resourceType,
						count,
						true,
						overrides
					);
				}
				
				public static PropertyKeyValue[] OfZero(
					string resourceType,
					params PropertyKeyValue[] overrides
				)
				{
					return Get(
						resourceType,
						0,
						false,
						overrides
					);
				}

				public static PropertyKeyValue[] Of(
					string resourceType,
					int count,
					params PropertyKeyValue[] overrides
				)
				{
					return Get(
						resourceType,
						count,
						false,
						overrides
					);
				}
				
				static PropertyKeyValue[] Get(
					string resourceType,
					int count,
					bool isCache,
					params PropertyKeyValue[] overrides
				)
				{
					return Combine(
						Values.Shared.Types.Capacity,
						new PropertyKeyValue[]
						{
							(Keys.Capacity.ResourceType, resourceType),
							(Keys.Capacity.IsCache, isCache),
							(Keys.Capacity.Desire, Values.Capacity.Desires.NotCalculated), 
							(Keys.Capacity.TimeoutExpired, 0L),
							(Keys.Capacity.CurrentCount, 0),
							(Keys.Capacity.MaximumCount, count),
							(Keys.Capacity.TargetCount, count)
						},
						overrides
					);
				}
			}

			public static class Reservation
			{
				public static PropertyKeyValue[] OfInput(
					string resourceType,
					long capacityId,
					params PropertyKeyValue[] overrides
				)
				{
					return Of(
						resourceType,
						capacityId,
						Values.Reservation.States.Input,
						overrides
					);
				}
				
				public static PropertyKeyValue[] OfOutput(
					string resourceType,
					long capacityId,
					params PropertyKeyValue[] overrides
				)
				{
					return Of(
						resourceType,
						capacityId,
						Values.Reservation.States.Output,
						overrides
					);
				}
				
				static PropertyKeyValue[] Of(
					string resourceType,
					long capacityId,
					string state,
					params PropertyKeyValue[] overrides
				)
				{
					return Combine(
						Values.Shared.Types.Reservation,
						new PropertyKeyValue[]
						{
							(Keys.Shared.IsPromised, false),
							
							(Keys.Reservation.ResourceType, resourceType),
							(Keys.Reservation.CapacityId, capacityId),
							(Keys.Reservation.ItemId, IdCounter.UndefinedId),
							(Keys.Reservation.State, state)
						},
						overrides
					);
				}
			}
			
			public static class Transfer
			{
				public static PropertyKeyValue[] Pickup(
					string resourceType,
					long inventoryId,
					long reservationId,
					long itemId,
					params PropertyKeyValue[] overrides
				)
				{
					return Of(
						resourceType,
						inventoryId,
						reservationId,
						itemId,
						Values.Shared.LogisticStates.Pickup,
						overrides
					);
				}
				
				public static PropertyKeyValue[] Dropoff(
					string resourceType,
					long inventoryId,
					long reservationId,
					long itemId,
					params PropertyKeyValue[] overrides
				)
				{
					return Of(
						resourceType,
						inventoryId,
						reservationId,
						itemId,
						Values.Shared.LogisticStates.Dropoff,
						overrides
					);
				}
				
				static PropertyKeyValue[] Of(
					string resourceType,
					long inventoryId,
					long reservationId,
					long itemId,
					string logisticsState,
					params PropertyKeyValue[] overrides
				)
				{
					return Combine(
						Values.Shared.Types.Transfer,
						new PropertyKeyValue[]
						{
							(Keys.Shared.LogisticsState, logisticsState),
							
							(Keys.Transfer.ResourceType, resourceType),
							(Keys.Transfer.InventoryId, inventoryId),
							(Keys.Transfer.ReservationId, reservationId),
							(Keys.Transfer.ItemId, itemId)
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
				public static readonly PropertyKey<string> LogisticsState = Create<string>(nameof(LogisticsState));
				public static readonly PropertyKey<bool> IsPromised = Create<bool>(nameof(IsPromised));
			}
			
			public static class Resource
			{
				static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Resource), suffix);

				public static readonly PropertyKey<string> Type = Create<string>(nameof(Type));

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
				
				public static readonly PropertyKey<string> ResourceType = Create<string>(nameof(ResourceType));
				public static readonly PropertyKey<bool> IsCache = Create<bool>(nameof(IsCache));
				public static readonly PropertyKey<string> Desire = Create<string>(nameof(Desire));
				public static readonly PropertyKey<long> TimeoutExpired = Create<long>(nameof(TimeoutExpired));
				public static readonly PropertyKey<int> CurrentCount = Create<int>(nameof(CurrentCount));
				public static readonly PropertyKey<int> MaximumCount = Create<int>(nameof(MaximumCount));
				public static readonly PropertyKey<int> TargetCount = Create<int>(nameof(TargetCount));
			}

			public static class Reservation
			{
				static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Reservation), suffix);
				
				public static readonly PropertyKey<string> ResourceType = Create<string>(nameof(ResourceType));
				public static readonly PropertyKey<long> CapacityId = Create<long>(nameof(CapacityId));
				public static readonly PropertyKey<long> ItemId = Create<long>(nameof(ItemId));
				public static readonly PropertyKey<string> State = Create<string>(nameof(State));
			}
			
			public static class Transfer
			{
				static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Transfer), suffix);
				
				public static readonly PropertyKey<long> ItemId = Create<long>(nameof(ItemId));
				public static readonly PropertyKey<long> InventoryId = Create<long>(nameof(InventoryId));
				public static readonly PropertyKey<long> ReservationId = Create<long>(nameof(ReservationId));
				public static readonly PropertyKey<string> ResourceType = Create<string>(nameof(ResourceType));
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
					public static readonly string Transfer = nameof(Transfer).ToSnakeCase();
				}
				
				public static class LogisticStates
				{
					public static readonly string None = nameof(None).ToSnakeCase();
					public static readonly string Input = nameof(Input).ToSnakeCase();
					public static readonly string Output = nameof(Output).ToSnakeCase();
					public static readonly string Pickup = nameof(Pickup).ToSnakeCase();
					public static readonly string Dropoff = nameof(Dropoff).ToSnakeCase();
				}
			}
			
			public static class Resource
			{
				public static class Types
				{
					public static readonly string Stalk = nameof(Stalk).ToSnakeCase();
				}
			}
			
			public static class Capacity
			{
				public static class Desires
				{
					public static readonly string NotCalculated = nameof(NotCalculated).ToSnakeCase();
					public static readonly string None = nameof(None).ToSnakeCase();
					public static readonly string Receive = nameof(Receive).ToSnakeCase();
					public static readonly string Distribute = nameof(Distribute).ToSnakeCase();
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