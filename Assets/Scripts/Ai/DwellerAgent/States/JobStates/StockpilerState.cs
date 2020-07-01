using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class StockpilerState<S> : JobState<S, StockpilerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "Job"+Job;

		protected override Jobs Job => Jobs.Stockpiler;

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
		
		#region Child Classes
		class CleanupState : CleanupItemDropsState<StockpilerState<S>, CleanupState> { }
		#endregion
	}
}