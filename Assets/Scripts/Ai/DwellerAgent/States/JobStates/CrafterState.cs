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
			base.OnInitialize();

			AddChildStates(
				// new InventoryRequestState(),
				new NavigateState(),
				// new BalanceItemState(),
				new CraftRecipeHandlerState()
			);

			AddTransitions(
				new ToReturnOnJobChanged(),
				new ToReturnOnShiftEnd(),
				new ToReturnOnWorkplaceMissing(),
				new ToReturnOnWorkplaceIsNotNavigable(),

				// new InventoryRequestState.ToInventoryRequestOnPromises(),
				
				new ToNavigateToWorkplace(),
				
				new CraftRecipeHandlerState.ToObligationOnExistingObligation(),
				new CraftRecipeHandlerState.ToObligationHandlerOnAvailableObligation()
				
				// new BalanceItemState.ToBalanceOnAvailableDelivery((s, d) => d.Enterable.IsOwner),
				// new BalanceItemState.ToBalanceOnAvailableDistribution((s, d) => s.Enterable.IsOwner)
			);
		}

		public override void Idle()
		{

			if (Workplace.Recipes.TryGetCurrent(out var current))
			{
				switch (current.State)
				{
					case RecipeComponent.States.Idle:
					case RecipeComponent.States.Gathering:
						Workplace.Recipes.ProcessRecipe();
						break;
					case RecipeComponent.States.Ready:
					case RecipeComponent.States.Crafting:
						break;
					default:
						Debug.LogError("Unrecognized Recipe State: "+current.State);
						break;
				}
			}
			else Workplace.Recipes.ProcessRecipe();
		}
		
		class CraftRecipeHandlerState : CraftRecipeHandlerState<S1> { }
	}
}