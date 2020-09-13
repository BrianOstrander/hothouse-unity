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
				public static readonly PropertyKeyValue[] Scrap = Create(Values.Resource.Types.Scrap);
			}
			
			public static class CapacityPool
			{
				public static PropertyKeyValue[] OfUnlimited(
					params PropertyKeyValue[] overrides
				)
				{
					return Get(
						int.MaxValue,
						overrides
					);
				}
				
				public static PropertyKeyValue[] OfZero(
					params PropertyKeyValue[] overrides
				)
				{
					return Get(
						0,
						overrides
					);
				}

				public static PropertyKeyValue[] Of(
					int count,
					params PropertyKeyValue[] overrides
				)
				{
					return Get(
						count,
						overrides
					);
				}
				
				static PropertyKeyValue[] Get(
					int countMaximum,
					params PropertyKeyValue[] overrides
				)
				{
					return Combine(
						Values.Shared.Types.CapacityPool,
						new PropertyKeyValue[]
						{
							(Keys.CapacityPool.CountCurrent, 0),
							(Keys.CapacityPool.CountMaximum, countMaximum)
						},
						overrides
					);
				}
			}
			
			public static class Capacity
			{
				public static PropertyKeyValue[] OfZero(
					long filterId,
					long poolId,
					params PropertyKeyValue[] overrides
				)
				{
					return Get(
						filterId,
						poolId,
						0,
						overrides
					);
				}

				public static PropertyKeyValue[] Of(
					long filterId,
					long poolId,
					int count,
					params PropertyKeyValue[] overrides
				)
				{
					return Get(
						filterId,
						poolId,
						count,
						overrides
					);
				}
				
				static PropertyKeyValue[] Get(
					long filterId,
					long poolId,
					int count,
					params PropertyKeyValue[] overrides
				)
				{
					return Combine(
						Values.Shared.Types.Capacity,
						new PropertyKeyValue[]
						{
							(Keys.Capacity.Filter, filterId),
							(Keys.Capacity.Pool, poolId),
							(Keys.Capacity.Desire, Values.Capacity.Desires.NotCalculated), 
							(Keys.Capacity.TimeoutExpired, 0L),
							(Keys.Capacity.CountCurrent, 0),
							(Keys.Capacity.CountMaximum, count),
							(Keys.Capacity.CountTarget, count)
						},
						overrides
					);
				}
			}

			public static class Reservation
			{
				static PropertyKeyValue[] Of(
					string type,
					long targetId,
					string state,
					params PropertyKeyValue[] overrides
				)
				{
					return Combine(
						Values.Shared.Types.Reservation,
						new PropertyKeyValue[]
						{
							(Keys.Reservation.TargetType, type),
							(Keys.Reservation.TargetId, targetId),
							(Keys.Reservation.TransferId, IdCounter.UndefinedId),
							(Keys.Reservation.IsPromised, false),
							(Keys.Reservation.LogisticState, state)
						},
						overrides
					);
				}

				public static class ForCapacity
				{
					public static PropertyKeyValue[] OfInput(
						long capacityId,
						params PropertyKeyValue[] overrides
					)
					{
						return Of(
							Values.Reservation.TargetTypes.Capacity,
							capacityId,
							Values.Reservation.LogisticStates.Input,
							overrides
						);
					}
				
					public static PropertyKeyValue[] OfOutput(
						long capacityId,
						params PropertyKeyValue[] overrides
					)
					{
						return Of(
							Values.Reservation.TargetTypes.Capacity,
							capacityId,
							Values.Reservation.LogisticStates.Output,
							overrides
						);
					}
				}

				public static class ForDrop
				{
					public static PropertyKeyValue[] OfOutput(
						long resourceId,
						params PropertyKeyValue[] overrides
					)
					{
						return Of(
							Values.Reservation.TargetTypes.Resource,
							resourceId,
							Values.Reservation.LogisticStates.Output,
							overrides
						);
					}
				}

				// public static PropertyKeyValue[] OfUnknown(
				// 	long capacityId,
				// 	params PropertyKeyValue[] overrides
				// )
				// {
				// 	return Of(
				// 		capacityId,
				// 		Values.Reservation.LogisticStates.Unknown,
				// 		overrides
				// 	);
				// }
			}
			
			public static class Transfer
			{
				public static PropertyKeyValue[] Pickup(
					long itemId,
					long reservationPickupId,
					long reservationDropoffId,
					params PropertyKeyValue[] overrides
				)
				{
					return Of(
						itemId,
						Values.Transfer.LogisticStates.Pickup,
						reservationPickupId,
						reservationDropoffId,
						overrides
					);
				}
				
				public static PropertyKeyValue[] Dropoff(
					long itemId,
					long reservationDropoffId,
					params PropertyKeyValue[] overrides
				)
				{
					return Of(
						itemId,
						Values.Transfer.LogisticStates.Dropoff,
						IdCounter.UndefinedId,
						reservationDropoffId,
						overrides
					);
				}
				
				static PropertyKeyValue[] Of(
					long itemId,
					string logisticState,
					long reservationPickupId,
					long reservationDropoffId,
					params PropertyKeyValue[] overrides
				)
				{
					return Combine(
						Values.Shared.Types.Transfer,
						new PropertyKeyValue[]
						{
							(Keys.Transfer.ItemId, itemId),
							(Keys.Transfer.LogisticState, logisticState),
							(Keys.Transfer.ReservationPickupId, reservationPickupId),
							(Keys.Transfer.ReservationDropoffId, reservationDropoffId)
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

				public static readonly PropertyKey<string> Type = Create<string>(nameof(Type));
				public static readonly PropertyKey<bool> IsPromised = Create<bool>(nameof(IsPromised));
				public static readonly PropertyKey<string> LogisticState = Create<string>(nameof(LogisticState));

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

			public static class CapacityPool
			{
				static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Capacity), suffix);
				
				public static readonly PropertyKey<int> CountCurrent = Create<int>(nameof(CountCurrent));
				public static readonly PropertyKey<int> CountMaximum = Create<int>(nameof(CountMaximum));
			}
			
			public static class Capacity
			{
				static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Capacity), suffix);
				
				public static readonly PropertyKey<long> Filter = Create<long>(nameof(Filter));
				public static readonly PropertyKey<long> Pool = Create<long>(nameof(Pool));
				public static readonly PropertyKey<string> Desire = Create<string>(nameof(Desire));
				public static readonly PropertyKey<long> TimeoutExpired = Create<long>(nameof(TimeoutExpired));
				public static readonly PropertyKey<int> CountCurrent = Create<int>(nameof(CountCurrent));
				public static readonly PropertyKey<int> CountMaximum = Create<int>(nameof(CountMaximum));
				public static readonly PropertyKey<int> CountTarget = Create<int>(nameof(CountTarget));
			}

			public static class Reservation
			{
				static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Reservation), suffix);
				
				public static readonly PropertyKey<string> TargetType = Create<string>(nameof(TargetType));
				public static readonly PropertyKey<long> TargetId = Create<long>(nameof(TargetId));
				public static readonly PropertyKey<long> TransferId = Create<long>(nameof(TransferId));
				public static readonly PropertyKey<bool> IsPromised = Create<bool>(nameof(IsPromised));
				public static readonly PropertyKey<string> LogisticState = Create<string>(nameof(LogisticState));
			}
			
			public static class Transfer
			{
				static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Transfer), suffix);
				
				public static readonly PropertyKey<long> ItemId = Create<long>(nameof(ItemId));
				public static readonly PropertyKey<string> LogisticState = Create<string>(nameof(LogisticState));
				public static readonly PropertyKey<long> ReservationPickupId = Create<long>(nameof(ReservationPickupId));
				public static readonly PropertyKey<long> ReservationDropoffId = Create<long>(nameof(ReservationDropoffId));
			}
		}

		public static class Values
		{
			public static class Shared
			{
				public static class Types
				{
					public static readonly string Resource = nameof(Resource).ToSnakeCase();
					public static readonly string CapacityPool = nameof(CapacityPool).ToSnakeCase();
					public static readonly string Capacity = nameof(Capacity).ToSnakeCase();
					public static readonly string Reservation = nameof(Reservation).ToSnakeCase();
					public static readonly string Transfer = nameof(Transfer).ToSnakeCase();
				}
			}
			
			public static class Resource
			{
				public static class Types
				{
					public static readonly string Stalk = nameof(Stalk).ToSnakeCase();
					public static readonly string Scrap = nameof(Scrap).ToSnakeCase();
				}
				
				public static class LogisticStates
				{
					public static readonly string None = nameof(None).ToSnakeCase();
					public static readonly string Output = nameof(Output).ToSnakeCase();
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
				public static class TargetTypes
				{
					public static readonly string Unknown = nameof(Unknown).ToSnakeCase();
					public static readonly string Resource = nameof(Resource).ToSnakeCase();
					public static readonly string Capacity = nameof(Capacity).ToSnakeCase();
				}
				
				public static class LogisticStates
				{
					public static readonly string Unknown = nameof(Unknown).ToSnakeCase();
					public static readonly string Input = nameof(Input).ToSnakeCase();
					public static readonly string Output = nameof(Output).ToSnakeCase();
				}
			}
			
			public static class Transfer
			{
				public static class LogisticStates
				{
					public static readonly string Pickup = nameof(Pickup).ToSnakeCase();
					public static readonly string Dropoff = nameof(Dropoff).ToSnakeCase();
				}
			}
		}
	}
}