using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;

namespace Lunra.WildVacuum.Ai
{
	public abstract class DwellerJobState<S> : AgentState<GameModel, DwellerModel>
		where S : DwellerJobState<S>
	{
		public override string Name => Job + "Job";

		public abstract DwellerModel.Jobs Job { get; }

		ToJobTransition toJobTransition;
		public ToJobTransition GetToJobTransition => toJobTransition ?? (toJobTransition = new ToJobTransition(this as S));
		
		public override void OnInitialize()
		{
			AddTransitions(new ToIdleTransition(this as S));
		}
		
		public class ToJobTransition : AgentTransition<S, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + jobState.Name + ">";

			S jobState;

			public ToJobTransition(S jobState) => this.jobState = jobState; 
			
			public override bool IsTriggered() => jobState.Job == Agent.Job.Value;
		}
		
		class ToIdleTransition : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + jobState.Name + ">";
			
			S jobState;

			public ToIdleTransition(S jobState) => this.jobState = jobState; 
			
			public override bool IsTriggered() => jobState.Job != Agent.Job.Value;
		}
	}
}