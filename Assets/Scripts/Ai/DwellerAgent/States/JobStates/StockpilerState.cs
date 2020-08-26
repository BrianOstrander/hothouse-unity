using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class StockpilerState<S> : JobState<S, StockpilerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		protected override Jobs Job => Jobs.Stockpiler;

		public override void OnInitialize()
		{
			base.OnInitialize();
			
			AddChildStates(
				// new InventoryRequestState(),
				new NavigateState()
				// new BalanceItemState()
			);

			AddTransitions(
				new ToReturnOnJobChanged(),
				new ToReturnOnShiftEnd(),
				new ToReturnOnWorkplaceMissing(),
				new ToReturnOnWorkplaceIsNotNavigable(),

				// new InventoryRequestState.ToInventoryRequestOnPromises(),
				
				new ToNavigateToWorkplace()
				
				// new BalanceItemState.ToBalanceOnAvailableDelivery((s, d) => s.Enterable.IsOwner),
				// new BalanceItemState.ToBalanceOnAvailableDistribution(ValidateDistribution)
			);
		}

		/*
		bool ValidateDistribution(
			BalanceItemState.ToBalanceOnAvailable.InventoryCache source,
			BalanceItemState.ToBalanceOnAvailable.InventoryCache destination
		)
		{
			if (source.Enterable.IsOwner) return true;
			if (!(destination.Enterable.Model is BuildingModel destinationBuildingModel)) return true;
			if (!destinationBuildingModel.IsBuildingState(BuildingStates.Operating)) return true;

			if (Workplaces.Contains(destinationBuildingModel.Type.Value)) return destination.Enterable.IsOwner;

			return true;
		}
		*/
	}
}