using Lunra.Core;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;

namespace Lunra.WildVacuum.Ai
{
	public abstract class DwellerJobState<S> : AgentState<GameModel, DwellerModel>
		where S : DwellerJobState<S>
	{
		public override string Name => Job + "Job";

		public abstract DwellerModel.Jobs Job { get; }

		ToJobOnShiftBegin toJobOnShiftBegin;
		public ToJobOnShiftBegin GetToJobOnShiftBegin => toJobOnShiftBegin ?? (toJobOnShiftBegin = new ToJobOnShiftBegin(this as S));
		
		public override void OnInitialize()
		{
			AddTransitions(
				new ToIdleOnJobUnassigned(this as S),
				new ToIdleOnShiftEnd(this as S)
			);
		}

		public class ToJobOnShiftBegin : AgentTransition<S, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + jobState.Name + ">";

			S jobState;

			public ToJobOnShiftBegin(S jobState) => this.jobState = jobState; 
			
			public override bool IsTriggered() => jobState.Job == Agent.Job.Value && Agent.JobShift.Value.Contains(World.SimulationTime.Value);
		}
		
		protected class ToIdleOnJobUnassigned : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + jobState.Name + ">";
			
			S jobState;

			public ToIdleOnJobUnassigned(S jobState) => this.jobState = jobState; 
			
			public override bool IsTriggered() => jobState.Job != Agent.Job.Value;
		}
		
		protected class ToIdleOnShiftEnd : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + jobState.Name + ">";
			
			S jobState;

			public ToIdleOnShiftEnd(S jobState) => this.jobState = jobState; 
			
			public override bool IsTriggered() => !Agent.JobShift.Value.Contains(World.SimulationTime.Value);

			public override void Transition()
			{
				switch (Agent.Desire.Value)
				{
					case Desires.Unknown:
					case Desires.None:
						Agent.Desire.Value = EnumExtensions.GetValues(Desires.Unknown, Desires.None).Random();
						break;
				}
			}
		}
	}
}