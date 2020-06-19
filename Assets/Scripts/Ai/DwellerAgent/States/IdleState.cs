using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class IdleState : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "Idle";

		public override void OnInitialize()
		{
			var timeoutState = new TimeoutState<IdleState>();
			
			AddChildStates(
				timeoutState	
			);
			AddTransitions(
				new DropItemsTransition<IdleState>(timeoutState)
			);
			
			InstantiateJob<ClearerJobState>();
			InstantiateJob<ConstructionJobState>();
			InstantiateDesire<SleepDesireState>();
			InstantiateDesire<EatDesireState>();
		}

		void InstantiateJob<S>()
			where S : JobState<S>, new()
		{
			var state = new S();
			AddChildStates(state);
			AddTransitions(state.GetToJobOnShiftBegin);
		}
		
		void InstantiateDesire<S>()
			where S : DesireState<S>, new()
		{
			var state = new S();
			AddChildStates(state);
			AddTransitions(new DesireState<S>.ToDesireOnShiftEnd(state));
		}
	}
}