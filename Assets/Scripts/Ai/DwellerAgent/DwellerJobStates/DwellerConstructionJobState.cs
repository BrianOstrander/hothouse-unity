using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai
{
	public class DwellerConstructionJobState : DwellerJobState<DwellerConstructionJobState>
	{
		public override Jobs Job => Jobs.Construction;

		public override void OnInitialize()
		{
			var validJobs = new[] { Jobs.Construction };
			var validItems = new[] { Item.Types.Stalks, Item.Types.Scrap };
			
			var transferItemsState = new DwellerTransferItemsState<DwellerConstructionJobState>();
			
			var cleanupState = new DwellerItemCleanupState<DwellerConstructionJobState>(
				validJobs,
				validItems
			);
			
			AddChildStates(
				transferItemsState,
				cleanupState,	
				new DwellerNavigateState<DwellerConstructionJobState>()
			);
			
			AddTransitions(
				new ToIdleOnJobUnassigned(this),
			
				new ToDepositToNearestConstructionSite(transferItemsState),

				new ToIdleOnShiftEnd(this),
				
				new ToNavigateToConstructionSite(),
				
				new ToItemCleanupOnValidInventory(
					cleanupState,
					ToItemCleanupOnValidInventory.InventoryTrigger.SomeOrNonZeroMaximumFull,
					validJobs,
					validItems
				),
				new ToItemCleanupOnValidInventory(
					cleanupState,
					ToItemCleanupOnValidInventory.InventoryTrigger.None,
					validJobs,
					validItems
				)
			);
		}

		/*
		class ToNavigateToWithdrawalItems : AgentTransition<DwellerNavigateState<DwellerConstructionJobState>, GameModel, DwellerModel>
		{
			public override bool IsTriggered()
			{
				// var constructionResourcesInNeed
				
				// var target = DwellerUtility.CalculateNearestEntrance(
				// 	Agent.Position.Value,
				// 	World.Buildings.AllActive,
				// 	b => b.BuildingState.Value == BuildingStates.Constructing,
				// 	out _,
				// 	out _
				// );

				// if (target == null) return false;
				return false;
			}
		}
		*/

		class ToDepositToNearestConstructionSite : AgentTransition<DwellerTransferItemsState<DwellerConstructionJobState>, GameModel, DwellerModel>
		{
			DwellerTransferItemsState<DwellerConstructionJobState> transferState;
			BuildingModel target;

			public ToDepositToNearestConstructionSite(
				DwellerTransferItemsState<DwellerConstructionJobState> transferState
			)
			{
				this.transferState = transferState;
			}
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation != InventoryPromise.Operations.Construction) return false;
				if (!Agent.Inventory.Value.Contains(Agent.InventoryPromise.Value.Inventory)) return false;
				
				target = World.Buildings.AllActive.FirstOrDefault(
					m =>
					{
						if (m.Id.Value != Agent.InventoryPromise.Value.BuildingId) return false;
						
						return m.Entrances.Value.Any(e => Mathf.Approximately(0f, Vector3.Distance(Agent.Position.Value.NewY(0f), e.Position.NewY(0f))));
					}
				);

				return target != null;
			}

			public override void Transition()
			{
				transferState.SetTarget(
					new DwellerTransferItemsState<DwellerConstructionJobState>.Target(
						i => target.ConstructionRecipe.Value = i,
						() => target.ConstructionRecipe.Value,
						i => Agent.Inventory.Value = i,
						() => Agent.Inventory.Value,
						Agent.InventoryPromise.Value.Inventory,
						Agent.DepositCooldown.Value
					)
				);
				
				Agent.InventoryPromise.Value = InventoryPromise.Default();
				// TODO: UNPROMISE STUFF HERE FROM BUILDING (WHEN THAT IS READY... or don't???)
			}
		}

		class ToNavigateToConstructionSite : AgentTransition<DwellerNavigateState<DwellerConstructionJobState>, GameModel, DwellerModel>
		{
			NavMeshPath path;
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation != InventoryPromise.Operations.Construction) return false;
				if (!Agent.Inventory.Value.Contains(Agent.InventoryPromise.Value.Inventory)) return false;
				
				var target = DwellerUtility.CalculateNearestEntrance(
					Agent.Position.Value,
					World.Buildings.AllActive,
					b =>
					{
						if (b.BuildingState.Value != BuildingStates.Constructing) return false;
						return b.Id.Value == Agent.InventoryPromise.Value.BuildingId;
					},
					out path,
					out _
				);

				return target != null;
			}

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(path);
		}
	}
}