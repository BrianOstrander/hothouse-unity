using System;
using Lunra.Hothouse.Models;
using UnityEngine;

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
				new BalanceItemState(),
				new CraftRecipeHandlerState()
			);

			AddTransitions(
				new ToReturnOnJobChanged(),
				new ToReturnOnShiftEnd(),
				new ToReturnOnWorkplaceMissing(),
				new ToReturnOnWorkplaceIsNotNavigable(),

				new InventoryRequestState.ToInventoryRequestOnPromises(),
				
				new ToNavigateToWorkplace(),
				
				new CraftRecipeHandlerState.ToObligationOnExistingObligation(),
				new CraftRecipeHandlerState.ToObligationHandlerOnAvailableObligation(),
				
				new BalanceItemState.ToBalanceOnAvailableDelivery(),
				new BalanceItemState.ToBalanceOnAvailableDistribution(),
				
				new CleanupState.ToCleanupOnItemsAvailable()
			);
		}

		public override void Idle()
		{
			switch (Workplace.Recipes.Current.Value.State)
			{
				case RecipeComponent.States.Idle:
				case RecipeComponent.States.Gathering:
					Workplace.Recipes.ProcessRecipe(Workplace);
					break;
				case RecipeComponent.States.Ready:
				case RecipeComponent.States.Crafting:
					break;
				default:
					Debug.LogError("Unrecognized Recipe State: "+Workplace.Recipes.Current.Value.State);
					break;
			}
		}
		
		class CraftRecipeHandlerState : CraftRecipeHandlerState<S1> { }
	}
}