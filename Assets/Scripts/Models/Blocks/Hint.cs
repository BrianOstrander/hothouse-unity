using System;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Assertions;

namespace Lunra.Hothouse.Models
{
	public struct Hint
	{
		static string GetUniqueId() => Guid.NewGuid().ToString();

		public static Hint NewDelay(float timeoutDuration)
		{
			return new Hint(
				GetUniqueId(),
				HintStates.Idle,
				null,
				Condition.Any(Condition.Types.ConstantTrue),
				DismissTriggers.Timeout,
				dismissTimeoutDuration: timeoutDuration
			);
		}
		
		public static Hint NewDismissedOnTimeout(
			string message,
			Condition activateCondition,
			float timeoutDuration = 10f
		)
		{
			return new Hint(
				GetUniqueId(),
				HintStates.Idle,
				message,
				activateCondition,
				DismissTriggers.Timeout,
				dismissTimeoutDuration: timeoutDuration
			);
		}
		
		public static Hint NewDismissedOnCondition(
			string message,
			Condition activateCondition,
			Condition dismissCondition
		)
		{
			return new Hint(
				GetUniqueId(),
				HintStates.Idle,
				message,
				activateCondition,
				DismissTriggers.Condition,
				dismissCondition: dismissCondition
			);
		}
		
		public static Hint NewDismissedOnConfirmed(
			string message,
			Condition activateCondition
		)
		{
			return new Hint(
				GetUniqueId(),
				HintStates.Idle,
				message,
				activateCondition,
				DismissTriggers.Confirmed
			);
		}
		
		public enum DismissTriggers
		{
			Unknown = 0,
			Condition = 10,
			Timeout = 20,
			Confirmed = 30
		}

		[JsonProperty] public string Id { get; private set; }
		[JsonProperty] public HintStates State { get; private set; }
		[JsonProperty] public string Message { get; private set; }
		[JsonProperty] readonly Condition activateCondition;

		[JsonProperty] readonly DismissTriggers dismissTrigger;

		[JsonProperty] readonly Condition dismissCondition;
		[JsonProperty] readonly float dismissTimeoutDuration;

		[JsonProperty] readonly TimeSpan dismissTimeoutExpires;

		[JsonIgnore] public bool IsDelay => dismissTrigger == DismissTriggers.Timeout && string.IsNullOrEmpty(Message);
		
		Hint(
			string id,
			HintStates state,
			string message,
			Condition activateCondition,
			DismissTriggers dismissTrigger,
			Condition dismissCondition = default,
			float dismissTimeoutDuration = 0f,
			TimeSpan dismissTimeoutExpires = default
		)
		{
			Assert.IsFalse(string.IsNullOrEmpty(id));
			Id = id;
			State = state;
			Message = message;
			this.activateCondition = activateCondition;
			this.dismissTrigger = dismissTrigger;
			this.dismissCondition = dismissCondition;
			this.dismissTimeoutDuration = dismissTimeoutDuration;
			this.dismissTimeoutExpires = dismissTimeoutExpires;
		}

		Hint NewState(
			HintStates state
		)
		{
			return new Hint(
				Id,
				state,
				Message,
				activateCondition,
				dismissTrigger,
				dismissCondition,
				dismissTimeoutDuration,
				dismissTimeoutExpires
			);
		}
		
		Hint NewStateAndDismissTimoutExpires(
			HintStates state,
			TimeSpan dismissTimeoutExpires
		)
		{
			return new Hint(
				Id,
				state,
				Message,
				activateCondition,
				dismissTrigger,
				dismissCondition,
				dismissTimeoutDuration,
				dismissTimeoutExpires
			);
		}
		
		public bool Evaluate(
			GameModel game,
			out Hint hintDelta
		)
		{
			hintDelta = this;
			
			switch (State)
			{
				case HintStates.Idle:
					if (activateCondition.Evaluate(game.Cache.Value))
					{
						if (dismissTrigger == DismissTriggers.Timeout)
						{
							hintDelta = NewStateAndDismissTimoutExpires(
								HintStates.Active,
								TimeSpan.FromSeconds(dismissTimeoutDuration) + game.PlaytimeElapsed.Value
							);
						}
						else hintDelta = NewState(HintStates.Active);
						
						return true;
					}
					break;
				case HintStates.Active:
					switch (dismissTrigger)
					{
						case DismissTriggers.Condition:
							if (dismissCondition.Evaluate(game.Cache.Value))
							{
								hintDelta = NewState(HintStates.Dismissed);
								return true;
							}
							break;
						case DismissTriggers.Timeout:
							if (dismissTimeoutExpires <= game.PlaytimeElapsed.Value)
							{
								hintDelta = NewState(HintStates.Dismissed);
								return true;
							}
							break;
						case DismissTriggers.Confirmed:
							if (game.Hints.ConfirmedHintIds.Value.Contains(Id))
							{
								hintDelta = NewState(HintStates.Dismissed);
								return true;
							}
							break;
						default:
							Debug.LogError("Unrecognized "+nameof(dismissTrigger)+": "+dismissTrigger);
							break;
					}
					break;
				case HintStates.Dismissed:
					return false;
				default:
					Debug.LogError("Unrecognized "+nameof(State)+": "+State);
					break;
			}

			return false;
		}
	}
}