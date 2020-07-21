using System;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public static class Damage
	{
		public enum Types
		{
			Unknown = 0,
			
			// Miscellaneous 
			Generic = 10,
			
			// Desire Damage
			Exhaustion = 100,
			Starvation = 110,
		}

		public static bool GetDamageTypeFromDesire(
			Motives motive,
			out Types type
		)
		{
			type = Types.Unknown;
			
			switch (motive)
			{
				case Motives.None:
					break;
				case Motives.Sleep:
					type = Types.Exhaustion;
					break;
				case Motives.Eat:
					type = Types.Starvation;
					break;
				default:
					Debug.LogError("Unrecognized desire: " + motive);
					break;
			}

			return type != Types.Unknown;
		}
		
		public static bool GetDesireFromDamageType(
			Types type,
			out Motives motive
		)
		{
			motive = Motives.Unknown;
			
			switch (type)
			{
				case Types.Exhaustion:
					motive = Motives.Sleep;
					break;
				case Types.Starvation:
					motive = Motives.Eat;
					break;
				default:
					Debug.LogError("Unrecognized desire: " + type);
					break;
			}

			return motive != Motives.Unknown;
		}

		public class Request
		{
			public static Request Generic(
				float amount,
				InstanceId source,
				InstanceId target = null
			)
			{
				return new Request(
					Types.Generic,
					amount,
					source,
					target ?? source
				);
			}
			
			public Types Type { get; }
			public float Amount { get; }

			public InstanceId Source { get; }
			public InstanceId Target { get; }
			
			public bool IsSelfInflicted { get; } 

			public Request(
				Types type,
				float amount,
				InstanceId source,
				InstanceId target = null
			)
			{
				Type = type;
				Amount = amount;
				Source = source;
				Target = target ?? source;
				IsSelfInflicted = Source.Id == Target.Id;
			}
		}

		public class Result
		{
			public Types Type { get; }
			/// <summary>
			/// The amount of damage the source wanted to inflict.
			/// </summary>
			public float AmountRequested { get; }
			/// <summary>
			/// The amount of damage actually applied before the Target died.
			/// </summary>
			public float AmountApplied { get; }
			/// <summary>
			/// The amount of damage absorbed by the Target, regardless of remaining health.
			/// </summary>
			public float AmountAbsorbed { get; }

			public InstanceId Source { get; }
			public InstanceId Target { get; }

			public bool IsSelfInflicted { get; }
			public bool IsTargetDestroyed { get; }

			public Result(
				Request request,
				float amountApplied,
				float amountAbsorbed,
				bool isTargetDestroyed
			)
			{
				Type = request.Type;
				AmountRequested = request.Amount;
				AmountApplied = amountApplied;
				AmountAbsorbed = amountAbsorbed;

				Source = request.Source;
				Target = request.Target;

				IsSelfInflicted = request.IsSelfInflicted;
				IsTargetDestroyed = isTargetDestroyed;
			}
		}

		public static Result ApplyGeneric(
			float amount,
			IHealthModel source,
			IHealthModel target = null
		)
		{
			return Apply(
				Types.Generic,
				amount,
				source,
				target
			);
		}
		
		public static Result Apply(
			Types type,
			float amount,
			IHealthModel source,
			IHealthModel target = null
		)
		{
			target = target ?? source;
			
			return target.Health.Damage(
				new Request(
					type,
					amount,
					source.GetInstanceId(),
					target.GetInstanceId()
				)
			);
		}
	}
}