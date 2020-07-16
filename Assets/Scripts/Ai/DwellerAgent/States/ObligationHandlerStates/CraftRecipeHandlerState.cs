using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class CraftRecipeHandlerState<S> : ObligationHandlerState<S, CraftRecipeHandlerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		static readonly Obligation[] DefaultObligationsHandled =
		{
			ObligationCategories.Craft.Recipe
		};

		public override Obligation[] ObligationsHandled => DefaultObligationsHandled;

		protected override bool RequiresOwnership => true;

		public override void OnInitialize()
		{
			AddChildStates(
				new NavigateState(),
				TimeoutInstance = new TimeoutState()
			);
			
			AddTransitions(
				new ToReturnOnMissingObligation(),
				new ToReturnOnTimeout(),
				
				new ToTimeoutOnCraftRecipe(),
				
				new ToNavigateToTarget()
			);
		}

		class ToTimeoutOnCraftRecipe : ToTimeoutOnTarget
		{
			IRecipeModel workplace;
			
			protected override void OnTimeoutBegin()
			{
				if (Agent.Workplace.Value.TryGetInstance(Game, out workplace))
				{
					workplace.Recipes.ProcessRecipe(Game, workplace);
				}
				else Debug.LogError("Unable to find workplace, this is unexpected");
			}

			protected override void OnTimeoutEnd()
			{
				if (workplace != null)
				{
					workplace.Recipes.ProcessRecipe(Game, workplace);
				}
				else Debug.LogError("Unable to find workplace, this is unexpected");
			}

			protected override bool CanPopObligation => true;
		}
	}
}