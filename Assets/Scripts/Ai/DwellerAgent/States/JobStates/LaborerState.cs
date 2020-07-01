using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class LaborerState<S> : JobState<S, LaborerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		protected override Jobs Job => Jobs.Laborer;

		public override void OnInitialize()
		{
			AddChildStates(
				new CleanupState()
			);
			
			AddTransitions(
				new ToReturnOnJobChanged(),
				new ToReturnOnShiftEnd(),
				
				new CleanupState.ToCleanupOnItemsAvailable()
			);
		}
	}
}