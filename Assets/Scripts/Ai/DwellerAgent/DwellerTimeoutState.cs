using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerTimeoutState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		public enum Types
		{
			Unknown = 0,
			Interval = 10,
			Time = 20
		}

		public override string Name => "Timeout";

		Types type;
		Interval interval;
		DayTime dayTime;
		
		public override void OnInitialize()
		{
			AddTransitions(
				new ToReturnOnIntervalElapsed(this),
				new ToReturnOnTimeElapsed(this),
				new ToReturnOnInvalid(this)
			);
		}

		void Reset()
		{
			type = Types.Unknown;
			interval = default;
			dayTime = default;
		}

		public void ConfigureForInterval(Interval interval)
		{
			Reset();
			type = Types.Interval;
			this.interval = interval;	
		}

		public void ConfigureForNextTimeOfDay(float timeOfDay)
		{
			Reset();
			type = Types.Time;
			dayTime = new DayTime(
				timeOfDay < World.SimulationTime.Value.Time ? World.SimulationTime.Value.Day + 1 : World.SimulationTime.Value.Day,
				timeOfDay
			);	
		}

		public void ConfigureForDayAndTime(DayTime atDayTime)
		{
			Reset();
			type = Types.Time;
			dayTime = DayTime.Max(World.SimulationTime.Value, atDayTime);
		}

		public override void Idle()
		{
			switch (type)
			{
				case Types.Interval:
					interval = interval.Update(World.SimulationDelta);
					break;
			}
		}

		class ToReturnOnIntervalElapsed : AgentTransition<S, GameModel, DwellerModel>
		{
			DwellerTimeoutState<S> sourceState;

			public ToReturnOnIntervalElapsed(DwellerTimeoutState<S> sourceState) => this.sourceState = sourceState;
			
			public override bool IsTriggered() => sourceState.type == Types.Interval && sourceState.interval.IsDone;
		}
		
		class ToReturnOnTimeElapsed : AgentTransition<S, GameModel, DwellerModel>
		{
			DwellerTimeoutState<S> sourceState;

			public ToReturnOnTimeElapsed(DwellerTimeoutState<S> sourceState) => this.sourceState = sourceState;
			
			public override bool IsTriggered() => sourceState.type == Types.Time && sourceState.dayTime <= World.SimulationTime.Value;
		}
		
		class ToReturnOnInvalid : AgentTransition<S, GameModel, DwellerModel>
		{
			DwellerTimeoutState<S> sourceState;

			public ToReturnOnInvalid(DwellerTimeoutState<S> sourceState) => this.sourceState = sourceState;

			public override bool IsTriggered() => sourceState.type == Types.Unknown;
		}
	}
}