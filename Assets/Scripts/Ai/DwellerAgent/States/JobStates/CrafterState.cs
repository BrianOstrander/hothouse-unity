using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public abstract class CrafterState<S0, S1> : JobState<S0, S1>
		where S0 : AgentState<GameModel, DwellerModel>
		where S1 : CrafterState<S0, S1>
	{
		public override void OnInitialize()
		{	
			AddChildStates(
				new CleanupState(),
				new InventoryRequestState(),
				new NavigateState(),
				new BalanceItemState()
			);

			AddTransitions(
				new ToReturnOnJobChanged(),
				new ToReturnOnShiftEnd(),
				new ToReturnOnWorkplaceMissing(),
				new ToReturnOnWorkplaceIsNotNavigable(),

				new InventoryRequestState.ToInventoryRequestOnPromises(),
				
				new ToNavigateToWorkplace(),
				
				
				
				new BalanceItemState.ToBalanceOnAvailableDelivery(),
				new BalanceItemState.ToBalanceOnAvailableDistribution(),
				
				new CleanupState.ToCleanupOnItemsAvailable()
			);
		}

		public override void Begin()
		{
			base.Begin();

			if (Workplace == null) return;

			if (Workplace.Recipes.Current.Value != null) return;
			if (!Workplace.Recipes.Queue.TryPeek(out var next)) return;

			Workplace.Recipes.Current.Value = Workplace.Recipes.Queue.Dequeue();

			Workplace.Inventory.Desired.Value = InventoryDesire.UnCalculated(Workplace.Recipes.Current.Value.InputItems);
		}
		
		// class ToTimeoutForCrafting : AgentTransition<CrafterState<S>>
	}
}