using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public abstract class JobState<S0, S1> : AgentState<GameModel, DwellerModel>
		where S0 : AgentState<GameModel, DwellerModel>
		where S1 : JobState<S0, S1>
	{
		public override string Name => "Job"+Job;

		protected abstract Jobs Job { get; }

		protected bool IsCurrentJob => Job == Agent.Job.Value;

		public class ToJobOnShiftBegin : AgentTransition<S0, S1, GameModel, DwellerModel>
		{
			Jobs? job;
			
			public override bool IsTriggered()
			{
				return Agent.JobShift.Value.Contains(Game.SimulationTime.Value);
				/*
				if (!job.HasValue)
				{
					
				}
				
				return job.Value == Agent.Job.Value && Agent.JobShift.Value.Contains(Game.SimulationTime.Value);
				*/
			}
		}

		protected class ToReturnOnJobChanged : AgentTransition<S1, S0, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => !SourceState.IsCurrentJob;
		}

		protected class ToReturnOnShiftEnd : AgentTransition<S1, S0, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => !Agent.JobShift.Value.Contains(Game.SimulationTime.Value);
		}
	}
}