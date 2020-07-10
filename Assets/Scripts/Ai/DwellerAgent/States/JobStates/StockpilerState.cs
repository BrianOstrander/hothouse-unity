using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class StockpilerState<S> : JobState<S, StockpilerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		static readonly Buildings[] StockpilerWorkplaces = 
		{
			Buildings.StartingWagon,
			Buildings.DepotSmall
		};
		
		protected override Jobs Job => Jobs.Stockpiler;

		protected override Buildings[] Workplaces => StockpilerWorkplaces;

		public override void OnInitialize()
		{
			AddChildStates(
				new CleanupState(),
				new InventoryRequestState()
			);

			AddTransitions(
				new ToReturnOnJobChanged(),
				new ToReturnOnShiftEnd(),

				new InventoryRequestState.ToInventoryRequestOnPromises(),
				
				new CleanupState.ToCleanupOnItemsAvailable()
			);
		}
	}
}