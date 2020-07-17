using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class FarmerState<S> : JobState<S, FarmerState<S>>
		where S : AgentState<GameModel, DwellerModel>
	{
		const float CalculateFloraObligationsDelay = 3f; 
		
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
				new BalanceItemState(),
				new DestroyMeleeHandlerState()
			);

			AddTransitions(
				new ToReturnOnJobChanged(),
				new ToReturnOnShiftEnd(),
				new ToReturnOnWorkplaceMissing(),
				new ToReturnOnWorkplaceIsNotNavigable(),

				new InventoryRequestState.ToInventoryRequestOnPromises(),
				
				new ToNavigateToWorkplace(),
				
				new DestroyMeleeHandlerState.ToObligationOnExistingObligation(),
				new ToDestroyOvergrownFlora(),
				
				new BalanceItemState.ToBalanceOnAvailableDelivery(),
				new BalanceItemState.ToBalanceOnAvailableDistribution(),
				
				new CleanupState.ToCleanupOnItemsAvailable()
			);
		}

		public override void Idle()
		{
			if (Workplace == null) return;

			if (Workplace.Farm.SelectedSeed != Inventory.Types.Unknown)
			{
				if (Workplace.Farm.Plots.None() || (Game.NavigationMesh.CalculationState.Value == NavigationMeshModel.CalculationStates.Completed && Workplace.Farm.LastUpdated < Game.NavigationMesh.LastUpdated.Value))
				{
					Workplace.Farm.CalculatePlots(Game, Workplace);
				}
				else if (CalculateFloraObligationsDelay < (DateTime.Now - Workplace.Farm.LastUpdated).TotalSeconds)
				{
					Workplace.Farm.CalculateFloraObligations(Game, Workplace);
				}
			}
		}

		class ToDestroyOvergrownFlora : DestroyMeleeHandlerState.ToObligationHandlerOnAvailableObligation
		{
			protected override bool IsObligationParentValid(IObligationModel obligationParent)
			{
				return SourceState.Workplace.Farm.Plots.Any(
					p => Vector3.Distance(obligationParent.Transform.Position.Value, p.Position) < p.Radius	
				);
			}
		}
	}
}