using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;

namespace Lunra.WildVacuum.Ai
{
	public abstract class DwellerJobState<S> : AgentState<GameModel, DwellerModel>
		where S : DwellerJobState<S>
	{
		public override string Name => Job + "Job";

		public abstract DwellerModel.Jobs Job { get; }

		ToJobOnAssigned toJobOnAssigned;
		public ToJobOnAssigned GetToJobOnAssigned => toJobOnAssigned ?? (toJobOnAssigned = new ToJobOnAssigned(this as S));
		
		public override void OnInitialize()
		{
			AddTransitions(new ToIdleOnJobUnassigned(this as S));
		}
		
		public class ToJobOnAssigned : AgentTransition<S, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + jobState.Name + ">";

			S jobState;

			public ToJobOnAssigned(S jobState) => this.jobState = jobState; 
			
			public override bool IsTriggered() => jobState.Job == Agent.Job.Value;
		}
		
		class ToIdleOnJobUnassigned : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + jobState.Name + ">";
			
			S jobState;

			public ToIdleOnJobUnassigned(S jobState) => this.jobState = jobState; 
			
			public override bool IsTriggered() => jobState.Job != Agent.Job.Value;
		}
	}
}