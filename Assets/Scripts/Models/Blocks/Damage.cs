using Newtonsoft.Json;
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
			
			// Motivation Damage
			GoalHurt = 100,
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
			
			[JsonProperty] public Types Type { get; private set; }
			[JsonProperty] public float Amount { get; private set; }

			[JsonProperty] public InstanceId Source { get; private set; }
			[JsonProperty] public InstanceId Target { get; private set; }
			
			[JsonProperty] public bool IsSelfInflicted { get; private set; } 

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
			[JsonProperty] public Types Type { get; private set; }
			/// <summary>
			/// The amount of damage the source wanted to inflict.
			/// </summary>
			[JsonProperty] public float AmountRequested { get; private set; }
			/// <summary>
			/// The amount of damage actually applied before the Target died.
			/// </summary>
			[JsonProperty] public float AmountApplied { get; private set; }
			/// <summary>
			/// The amount of damage absorbed by the Target, regardless of remaining health.
			/// </summary>
			[JsonProperty] public float AmountAbsorbed { get; private set; }

			[JsonProperty] public InstanceId Source { get; private set; }
			[JsonProperty] public InstanceId Target { get; private set; }

			[JsonProperty] public bool IsSelfInflicted { get; private set; }
			[JsonProperty] public bool IsTargetDestroyed { get; private set; }

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