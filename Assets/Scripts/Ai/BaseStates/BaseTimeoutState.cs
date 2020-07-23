using System;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public class BaseTimeoutState<S, A> : AgentState<GameModel, A>
		where S : AgentState<GameModel, A>
		where A : AgentModel 
	{
		public enum Types
		{
			Unknown = 0,
			Interval = 10,
			Time = 20
		}

		public override string Name => "Timeout";

		public struct Configuration
		{
			public Types TimeoutType;
			public Interval TimeoutInterval;
			public DayTime TimeoutBeginDayTime;
			public DayTime TimeoutDayTime;

			public Action<(float Progress, bool IsDone)> Updated;
		}

		Configuration configuration;
		
		public override void OnInitialize()
		{
			AddTransitions(
				new ToReturnOnIntervalElapsed(),
				new ToReturnOnTimeElapsed(),
				new ToReturnOnInvalid()
			);
		}

		void Reset()
		{
			configuration = default;
		}

		public void ConfigureForInterval(
			Interval interval,
			Action<(float Progress, bool IsDone)> updated = null
		)
		{
			Reset();
			configuration.TimeoutType = Types.Interval;
			configuration.TimeoutInterval = interval;
			
			configuration.Updated = updated;
		}

		public void ConfigureForNextTimeOfDay(
			float timeOfDay,
			Action<(float Progress, bool IsDone)> updated = null
		)
		{
			Reset();
			configuration.TimeoutType = Types.Time;
			configuration.TimeoutBeginDayTime = Game.SimulationTime.Value;
			configuration.TimeoutDayTime = new DayTime(
				timeOfDay < Game.SimulationTime.Value.Time ? Game.SimulationTime.Value.Day + 1 : Game.SimulationTime.Value.Day,
				timeOfDay
			);

			configuration.Updated = updated;
		}

		public void ConfigureForDayAndTime(
			DayTime atDayTime,
			Action<(float Progress, bool IsDone)> updated = null
		)
		{
			Reset();
			configuration.TimeoutType = Types.Time;
			configuration.TimeoutBeginDayTime = Game.SimulationTime.Value;
			configuration.TimeoutDayTime = DayTime.Max(Game.SimulationTime.Value, atDayTime);

			configuration.Updated = updated;
		}

		public override void Begin()
		{
			if (configuration.TimeoutType == Types.Unknown)
			{
				Debug.LogError("Unable to process this timeout, are you sure you configured it before entering?");
			}
		}

		public override void Idle()
		{
			var progress = 0f;
			switch (configuration.TimeoutType)
			{
				case Types.Interval:
					configuration.TimeoutInterval = configuration.TimeoutInterval.Update(Game.SimulationTimeDelta);
					progress = configuration.TimeoutInterval.Normalized;
					break;
				case Types.Time:
					// TODO: Does this really return a negative ever? that's probably not great...
					var total = Mathf.Abs((configuration.TimeoutDayTime - configuration.TimeoutBeginDayTime).TotalTime);
					var remaining = Mathf.Abs((configuration.TimeoutDayTime - Game.SimulationTime.Value).TotalTime);
					// TODO: This isn't super tested...
					progress = 1f - (Mathf.Approximately(0f, total) ? 1f : Mathf.Min(remaining / total, 1f));
					break;
				default:
					Debug.LogError("Unrecognized Type: " + configuration.TimeoutType);
					break;
			}

			configuration.Updated?.Invoke((progress, false));
		}

		public override void End()
		{
			var finalUpdated = configuration.Updated;
			
			Reset();
			
			finalUpdated?.Invoke((1f, true));
		}

		class ToReturnOnIntervalElapsed : AgentTransition<BaseTimeoutState<S, A>, S, GameModel, A>
		{
			public override bool IsTriggered() => SourceState.configuration.TimeoutType == Types.Interval && SourceState.configuration.TimeoutInterval.IsDone;
		}
		
		class ToReturnOnTimeElapsed : AgentTransition<BaseTimeoutState<S, A>, S, GameModel, A>
		{
			public override bool IsTriggered() => SourceState.configuration.TimeoutType == Types.Time && SourceState.configuration.TimeoutDayTime <= Game.SimulationTime.Value;
		}
		
		class ToReturnOnInvalid : AgentTransition<BaseTimeoutState<S, A>, S, GameModel, A>
		{
			public override bool IsTriggered() => SourceState.configuration.TimeoutType == Types.Unknown;
		}
	}
}