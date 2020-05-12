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
		static BuildingModel GetNearestItemSource(
			GameModel world,
			DwellerModel agent,
			out NavMeshPath path,
			out Vector3 entrancePosition,
			out InventoryPromise promise 
		)
		{
			var promiseResult = InventoryPromise.Default();
			
			var validConstructionSites = world.Buildings.AllActive
				.Where(
					possibleConstructionSite =>
					{
						if (possibleConstructionSite.BuildingState.Value != BuildingStates.Constructing) return false;

						return !possibleConstructionSite.ConstructionRecipeInventory.Value
							.Inverted()
							.Subtract(possibleConstructionSite.ConstructionRecipeInventoryPromised.Value)
							.IsEmpty;
					}
				)
				.ToDictionary(b => b, b => false);

			var itemSourceResult = DwellerUtility.CalculateNearestOperatingEntrance(
				agent.Position.Value,
				out path,
				out entrancePosition,
				possibleItemSource =>
				{
					if (possibleItemSource.Inventory.Value.IsEmpty) return false;
					if (!possibleItemSource.InventoryPermission.Value.CanWithdrawal(agent.Job.Value)) return false;

					var index = 0;
					while (index < validConstructionSites.Count)
					{
						var kv = validConstructionSites.ElementAt(index);
						if (!kv.Value)
						{
							var navigationValid = DwellerUtility.CalculateNearestEntrance(
								agent.Position.Value,
								out _,
								out _,
								kv.Key
							);

							if (navigationValid) validConstructionSites[kv.Key] = true;
							else
							{
								validConstructionSites.Remove(kv.Key);
								continue;
							}
						}

						var nonPromisedInventory = kv.Key.ConstructionRecipeInventory.Value
							.Inverted()
							.Subtract(kv.Key.ConstructionRecipeInventoryPromised.Value);

						if (!nonPromisedInventory.Intersect(possibleItemSource.Inventory.Value).IsEmpty)
						{
							promiseResult = new InventoryPromise(
								kv.Key.Id.Value,
								InventoryPromise.Operations.Construction,
								nonPromisedInventory.Clamped(agent.Inventory.Value).SumClamped(agent.SharedInventoryMaximum.Value)
							); 
							return true;
						}

						index++;
					}

					return false;	
				},
				world.Buildings.AllActive
			);

			promise = promiseResult;
			
			return itemSourceResult;
		}

		enum Steps
		{
			Unknown = 0,
			Withdrawing = 10,
			Depositing = 20
		}
		
		public override Jobs Job => Jobs.Construction;

		Steps step;
		
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
			
				new ToWithdrawalItems(this, transferItemsState),
				new ToDepositToNearestConstructionSite(this, transferItemsState),
				new ToNavigateToConstructionSite(),

				new ToIdleOnShiftEnd(this),
				
				new ToNavigateToWithdrawalItems(),
				
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

		public override void Begin()
		{
			switch (step)
			{
				case Steps.Unknown:
					return;
			}
			
			var constructionSite = World.Buildings.AllActive.FirstOrDefault(b => b.Id.Value == Agent.InventoryPromise.Value.BuildingId);
			
			if (constructionSite == null)
			{
				// Building must have been destroyed...
				Agent.InventoryPromise.Value = InventoryPromise.Default();
				step = Steps.Unknown;
				return;
			}
			
			switch (step)
			{
				case Steps.Withdrawing:
					if (!Agent.Inventory.Value.Contains(Agent.InventoryPromise.Value.Inventory))
					{
						// The dweller was unable to pull all the resources it wanted to, so we're going to correct the
						// amount we promised
						var newPromise = Agent.InventoryPromise.Value.Inventory.Intersect(Agent.Inventory.Value);
						constructionSite.ConstructionRecipeInventoryPromised.Value = constructionSite.ConstructionRecipeInventoryPromised.Value
							.Subtract(Agent.InventoryPromise.Value.Inventory.Subtract(newPromise));

						Agent.InventoryPromise.Value = Agent.InventoryPromise.Value.NewInventory(newPromise);
					}
					break;
				case Steps.Depositing:
					constructionSite.ConstructionRecipeInventoryPromised.Value = constructionSite.ConstructionRecipeInventoryPromised.Value
						.Subtract(Agent.InventoryPromise.Value.Inventory);
					Agent.InventoryPromise.Value = InventoryPromise.Default();
					break;
			}
			
			step = Steps.Unknown;
		}

		class ToWithdrawalItems : AgentTransition<DwellerTransferItemsState<DwellerConstructionJobState>, GameModel, DwellerModel>
		{
			DwellerConstructionJobState sourceState;
			DwellerTransferItemsState<DwellerConstructionJobState> transferState;
			BuildingModel target;
			InventoryPromise promise;

			public ToWithdrawalItems(
				DwellerConstructionJobState sourceState,
				DwellerTransferItemsState<DwellerConstructionJobState> transferState
			)
			{
				this.sourceState = sourceState;
				this.transferState = transferState;
			}
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation != InventoryPromise.Operations.None) return false;

				target = GetNearestItemSource(
					World,
					Agent,
					out _,
					out var entrancePosition,
					out promise
				);

				if (target == null) return false;
				
				return Mathf.Approximately(0f, Vector3.Distance(Agent.Position.Value.NewY(0f), entrancePosition.NewY(0f)));
			}

			public override void Transition()
			{
				var constructionSite = World.Buildings.AllActive.First(b => b.Id.Value == promise.BuildingId);
				
				Agent.InventoryPromise.Value = promise;
				constructionSite.ConstructionRecipeInventoryPromised.Value = constructionSite.ConstructionRecipeInventoryPromised.Value.Add(promise.Inventory);

				transferState.SetTarget(
					new DwellerTransferItemsState<DwellerConstructionJobState>.Target(
						i => Agent.Inventory.Value = i,
						() => Agent.Inventory.Value,
						i => target.Inventory.Value = i,
						() => target.Inventory.Value,
						promise.Inventory,
						Agent.WithdrawalCooldown.Value
					)
				);

				sourceState.step = Steps.Withdrawing;
			}
		}

		class ToNavigateToWithdrawalItems : AgentTransition<DwellerNavigateState<DwellerConstructionJobState>, GameModel, DwellerModel>
		{
			NavMeshPath path;
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation != InventoryPromise.Operations.None) return false;

				var target = GetNearestItemSource(
					World,
					Agent,
					out path,
					out _,
					out _
				);

				return target != null;
			}

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(path);
		}

		class ToDepositToNearestConstructionSite : AgentTransition<DwellerTransferItemsState<DwellerConstructionJobState>, GameModel, DwellerModel>
		{
			DwellerConstructionJobState sourceState;
			DwellerTransferItemsState<DwellerConstructionJobState> transferState;
			BuildingModel target;

			public ToDepositToNearestConstructionSite(
				DwellerConstructionJobState sourceState,
				DwellerTransferItemsState<DwellerConstructionJobState> transferState
			)
			{
				this.sourceState = sourceState;
				this.transferState = transferState;
			}
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation != InventoryPromise.Operations.Construction) return false;
				if (!Agent.Inventory.Value.Contains(Agent.InventoryPromise.Value.Inventory))
				{
					Debug.LogError("This should not be able to happen!");
					return false;
				}
				
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
						i => target.ConstructionRecipeInventory.Value = i,
						() => target.ConstructionRecipeInventory.Value,
						i => Agent.Inventory.Value = i,
						() => Agent.Inventory.Value,
						Agent.InventoryPromise.Value.Inventory,
						Agent.DepositCooldown.Value
					)
				);

				sourceState.step = Steps.Depositing;
			}
		}

		class ToNavigateToConstructionSite : AgentTransition<DwellerNavigateState<DwellerConstructionJobState>, GameModel, DwellerModel>
		{
			NavMeshPath path;
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation != InventoryPromise.Operations.Construction) return false;

				var target = DwellerUtility.CalculateNearestEntrance(
					Agent.Position.Value,
					out path,
					out _,
					b =>
					{
						if (b.BuildingState.Value != BuildingStates.Constructing) return false;
						return b.Id.Value == Agent.InventoryPromise.Value.BuildingId;
					},
					World.Buildings.AllActive
				);

				return target != null;
			}

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(path);
		}
	}
}