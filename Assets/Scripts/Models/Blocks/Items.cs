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
							(Keys.Resource.LogisticState, Values.Resource.LogisticStates.None),
							(Keys.Resource.CapacityPoolId, IdCounter.UndefinedId),
							
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
					string type,
					params PropertyKeyValue[] overrides
				)
				{
					return Create(
						type,
						int.MaxValue,
						overrides
					);
				}
				
				public static PropertyKeyValue[] OfZero(
					string type,
					params PropertyKeyValue[] overrides
				)
				{
					return Create(
						type,
						0,
						overrides
					);
				}

				public static PropertyKeyValue[] Of(
					string type,
					int count,
					params PropertyKeyValue[] overrides
				)
				{
					return Create(
						type,
						count,
						overrides
					);
				}
				
				static PropertyKeyValue[] Create(
					string type,
					int countMaximum,
					params PropertyKeyValue[] overrides
				)
				{
					return Combine(
						Values.Shared.Types.CapacityPool,
						new PropertyKeyValue[]
						{
							(Keys.CapacityPool.Type, type),
							(Keys.CapacityPool.Priority, 0),
							(Keys.CapacityPool.CountCurrent, 0),
							(Keys.CapacityPool.CountMaximum, countMaximum),
							(Keys.CapacityPool.CountTarget, countMaximum),
							(Keys.CapacityPool.IsForbidden, false)
						},
						overrides
					);
				}
			}
			
			public static class Capacity
			{
				public static PropertyKeyValue[] OfZero(
					long poolId,
					params PropertyKeyValue[] overrides
				)
				{
					return Create(
						poolId,
						0,
						overrides
					);
				}

				public static PropertyKeyValue[] Of(
					long poolId,
					int count,
					params PropertyKeyValue[] overrides
				)
				{
					return Create(
						poolId,
						count,
						overrides
					);
				}
				
				static PropertyKeyValue[] Create(
					long poolId,
					int count,
					params PropertyKeyValue[] overrides
				)
				{
					return Combine(
						Values.Shared.Types.Capacity,
						new PropertyKeyValue[]
						{
							(Keys.Capacity.CapacityPoolId, poolId),
							(Keys.Capacity.Desire, Values.Capacity.Desires.NotCalculated),
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
					long capacityId,
					long capacityPoolId,
					string state,
					params PropertyKeyValue[] overrides
				)
				{
					return Combine(
						Values.Shared.Types.Reservation,
						new PropertyKeyValue[]
						{
							(Keys.Reservation.CapacityId, capacityId),
							(Keys.Reservation.CapacityPoolId, capacityPoolId),
							(Keys.Reservation.TransferId, IdCounter.UndefinedId),
							(Keys.Reservation.LogisticState, state)
						},
						overrides
					);
				}

				public static PropertyKeyValue[] OfInput(
					long capacityId,
					long capacityPoolId,
					params PropertyKeyValue[] overrides
				)
				{
					return Of(
						capacityId,
						capacityPoolId,
						Values.Reservation.LogisticStates.Input,
						overrides
					);
				}
				
				public static PropertyKeyValue[] OfOutput(
					long capacityId,
					long capacityPoolId,
					params PropertyKeyValue[] overrides
				)
				{
					return Of(
						capacityId,
						capacityPoolId,
						Values.Reservation.LogisticStates.Output,
						overrides
					);
				}

				public static PropertyKeyValue[] OfUnknown(
					long capacityId,
					long capacityPoolId,
					params PropertyKeyValue[] overrides
				)
				{
					return Of(
						capacityId,
						capacityPoolId,
						Values.Reservation.LogisticStates.Unknown,
						overrides
					);
				}
			}
			
			public static class Transfer
			{
				public static PropertyKeyValue[] Pickup(
					long itemId,
					long reservationOutputId,
					long reservationInputId,
					params PropertyKeyValue[] overrides
				)
				{
					return Of(
						itemId,
						Values.Transfer.ReservationTargets.Output,
						reservationOutputId,
						reservationInputId,
						overrides
					);
				}
				
				public static PropertyKeyValue[] Dropoff(
					long itemId,
					long reservationInputId,
					params PropertyKeyValue[] overrides
				)
				{
					return Of(
						itemId,
						Values.Transfer.ReservationTargets.Input,
						IdCounter.UndefinedId,
						reservationInputId,
						overrides
					);
				}
				
				static PropertyKeyValue[] Of(
					long itemId,
					string logisticState,
					long reservationOutputId,
					long reservationInputId,
					params PropertyKeyValue[] overrides
				)
				{
					return Combine(
						Values.Shared.Types.Transfer,
						new PropertyKeyValue[]
						{
							(Keys.Transfer.ItemId, itemId),
							(Keys.Transfer.ReservationTarget, logisticState),
							(Keys.Transfer.ReservationOutputId, reservationOutputId),
							(Keys.Transfer.ReservationInputId, reservationInputId)
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
				public static readonly PropertyKey<string> LogisticState = Create<string>(nameof(LogisticState));
				public static readonly PropertyKey<long> CapacityPoolId = Create<long>(nameof(CapacityPoolId));

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
				static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(CapacityPool), suffix);
				
				public static readonly PropertyKey<string> Type = Create<string>(nameof(Type));
				public static readonly PropertyKey<int> Priority = Create<int>(nameof(Priority));
				public static readonly PropertyKey<int> CountCurrent = Create<int>(nameof(CountCurrent));
				public static readonly PropertyKey<int> CountMaximum = Create<int>(nameof(CountMaximum));
				public static readonly PropertyKey<int> CountTarget = Create<int>(nameof(CountTarget));
				public static readonly PropertyKey<bool> IsForbidden = Create<bool>(nameof(IsForbidden));
			}
			
			public static class Capacity
			{
				static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Capacity), suffix);
				
				public static readonly PropertyKey<long> CapacityPoolId = Create<long>(nameof(CapacityPoolId));
				public static readonly PropertyKey<string> Desire = Create<string>(nameof(Desire));
				public static readonly PropertyKey<int> CountCurrent = Create<int>(nameof(CountCurrent));
				public static readonly PropertyKey<int> CountMaximum = Create<int>(nameof(CountMaximum));
				public static readonly PropertyKey<int> CountTarget = Create<int>(nameof(CountTarget));
			}

			public static class Reservation
			{
				static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Reservation), suffix);
				
				public static readonly PropertyKey<long> CapacityId = Create<long>(nameof(CapacityId));
				public static readonly PropertyKey<long> CapacityPoolId = Create<long>(nameof(CapacityPoolId));
				public static readonly PropertyKey<long> TransferId = Create<long>(nameof(TransferId));
				public static readonly PropertyKey<string> LogisticState = Create<string>(nameof(LogisticState));
			}
			
			public static class Transfer
			{
				static PropertyKey<T> Create<T>(string suffix) => CreateKey<T>(nameof(Transfer), suffix);
				
				public static readonly PropertyKey<long> ItemId = Create<long>(nameof(ItemId));
				public static readonly PropertyKey<string> ReservationTarget = Create<string>(nameof(ReservationTarget));
				public static readonly PropertyKey<long> ReservationOutputId = Create<long>(nameof(ReservationOutputId));
				public static readonly PropertyKey<long> ReservationInputId = Create<long>(nameof(ReservationInputId));
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
					public static readonly string Transfer = nameof(Transfer).ToSnakeCase();
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
			
			public static class CapacityPool
			{
				public static class Types
				{
					public static readonly string Unknown = nameof(Unknown).ToSnakeCase();
					public static readonly string Drop = nameof(Drop).ToSnakeCase();
					public static readonly string Construction = nameof(Construction).ToSnakeCase();
					public static readonly string Stockpile = nameof(Stockpile).ToSnakeCase();
					public static readonly string Salvage = nameof(Salvage).ToSnakeCase();
				}
			}
			
			public static class Reservation
			{
				public static class LogisticStates
				{
					public static readonly string Unknown = nameof(Unknown).ToSnakeCase();
					public static readonly string Input = nameof(Input).ToSnakeCase();
					public static readonly string Output = nameof(Output).ToSnakeCase();
				}
			}
			
			public static class Transfer
			{
				public static class ReservationTargets
				{
					public static readonly string Output = nameof(Output).ToSnakeCase();
					public static readonly string Input = nameof(Input).ToSnakeCase();
				}
			}
		}
	}
}