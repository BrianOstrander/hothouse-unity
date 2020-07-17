using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class FarmerState<S> : JobState<S, FarmerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		protected override Jobs Job => Jobs.Farmer;

		public override void OnInitialize()
		{
			Workplaces = new []
			{
				Game.Buildings.GetDefinitionType<SeedSiloDefinition>()	
			};
			
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
	}
}