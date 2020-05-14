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

						return possibleConstructionSite.ConstructionInventoryCapacity.Value.IsNotFull(possibleConstructionSite.ConstructionInventory.Value + possibleConstructionSite.ConstructionInventoryPromised.Value);
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

						var nonPromisedInventory = kv.Key.ConstructionInventoryCapacity.Value.GetCapacityFor(
							kv.Key.ConstructionInventory.Value + kv.Key.ConstructionInventoryPromised.Value
						);

						if (nonPromisedInventory.Intersects(possibleItemSource.Inventory.Value, out var intersection))
						{
							agent.InventoryCapacity.Value.GetClamped(
								intersection,
								out var promisedInventory
							);
							
							promiseResult = new InventoryPromise(
								kv.Key.Id.Value,
								InventoryPromise.Operations.ConstructionDeposit,
								promisedInventory
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
			WithdrawingItemsFromCache = 10
		}
		
		public override Jobs Job => Jobs.Construction;

		Steps step;
		
		public override void OnInitialize()
		{
			var validJobs = new[] { Jobs.Construction, Jobs.None };
			var validCleanupItems = Inventory.ValidTypes;
			
			var transferItemsState = new DwellerTransferItemsState<DwellerConstructionJobState>();
			var timeoutState = new DwellerTimeoutState<DwellerConstructionJobState>();
			
			var cleanupState = new DwellerItemCleanupState<DwellerConstructionJobState>(
				validJobs,
				validCleanupItems
			);
			
			AddChildStates(
				transferItemsState,
				timeoutState,
				cleanupState,
				new DwellerNavigateState<DwellerConstructionJobState>()
			);
			
			AddTransitions(
				new ToIdleOnJobUnassigned(this),
			
				new ToWithdrawalItemsFromCache(this, transferItemsState),
				new ToDepositToNearestConstructionSite(this, transferItemsState),
				new ToWithdrawalItemsFromSalvageSite(this, transferItemsState),
				new ToNavigateToSalvageSite(),
				new ToNavigateToConstructionSite(),

				new ToIdleOnShiftEnd(this),
				
				new ToNavigateToWithdrawalItemsFromCache(),
				
				new ToItemCleanupOnValidInventory(
					cleanupState,
					ToItemCleanupOnValidInventory.InventoryTrigger.OnGreaterThanZero,
					validJobs,
					validCleanupItems
				),
				new ToItemCleanupOnValidInventory(
					cleanupState,
					ToItemCleanupOnValidInventory.InventoryTrigger.OnEmpty,
					validJobs,
					validCleanupItems
				),
				new ToDropItems(timeoutState)
			);
		}

		public override void Begin()
		{
			switch (step)
			{
				case Steps.Unknown:
					return;
			}
			
			var constructionSite = World.Buildings.AllActive.FirstOrDefault(b => b.Id.Value == Agent.InventoryPromise.Value.TargetId);
			
			if (constructionSite == null)
			{
				// Building must have been destroyed...
				Agent.InventoryPromise.Value = InventoryPromise.Default();
				step = Steps.Unknown;
				return;
			}
			
			switch (step)
			{
				case Steps.WithdrawingItemsFromCache:
					if (!Agent.Inventory.Value.Contains(Agent.InventoryPromise.Value.Inventory))
					{
						// The dweller was unable to pull all the resources it wanted to, so we're going to correct the
						// amount we promised
						Agent.InventoryPromise.Value.Inventory.Intersects(
							Agent.Inventory.Value,
							out var newPromise
						);
						constructionSite.ConstructionInventoryPromised.Value -= Agent.InventoryPromise.Value.Inventory - newPromise;

						Agent.InventoryPromise.Value = Agent.InventoryPromise.Value.NewInventory(newPromise);
					}
					break;
			}
			
			step = Steps.Unknown;
		}
		
		class ToWithdrawalItemsFromSalvageSite : AgentTransition<DwellerTransferItemsState<DwellerConstructionJobState>, GameModel, DwellerModel>
		{
			DwellerConstructionJobState sourceState;
			DwellerTransferItemsState<DwellerConstructionJobState> transferState;
			BuildingModel target;
			Inventory itemsToLoad;
			
			public ToWithdrawalItemsFromSalvageSite(
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
				if (Agent.InventoryCapacity.Value.IsFull(Agent.Inventory.Value)) return false;
				
				target = DwellerUtility.CalculateNearestEntrance(
					Agent.Position.Value,
					out _,
					out var entrancePosition,
					salvageSite =>
					{
						if (salvageSite.BuildingState.Value != BuildingStates.Salvaging) return false;
						if (salvageSite.SalvageInventory.Value.IsEmpty) return false;
						itemsToLoad = salvageSite.SalvageInventory.Value;
						return true;
					},
					World.Buildings.AllActive
				);

				if (target == null) return false;
				
				return Mathf.Approximately(0f, Vector3.Distance(Agent.Position.Value.NewY(0f), entrancePosition.NewY(0f)));
			}

			public override void Transition()
			{
				transferState.SetTarget(
					new DwellerTransferItemsState<DwellerConstructionJobState>.Target(
						i => Agent.Inventory.Value = i,
						() => Agent.Inventory.Value,
						i => Agent.InventoryCapacity.Value.GetCapacityFor(Agent.Inventory.Value, i),
						i => target.SalvageInventory.Value = i,
						() => target.SalvageInventory.Value,
						itemsToLoad,
						Agent.WithdrawalCooldown.Value
					)
				);
			}
		}

		class ToWithdrawalItemsFromCache : AgentTransition<DwellerTransferItemsState<DwellerConstructionJobState>, GameModel, DwellerModel>
		{
			DwellerConstructionJobState sourceState;
			DwellerTransferItemsState<DwellerConstructionJobState> transferState;
			BuildingModel target;
			InventoryPromise promise;

			public ToWithdrawalItemsFromCache(
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
				var constructionSite = World.Buildings.AllActive.First(b => b.Id.Value == promise.TargetId);
				
				Agent.InventoryPromise.Value = promise;
				constructionSite.ConstructionInventoryPromised.Value += promise.Inventory;

				transferState.SetTarget(
					new DwellerTransferItemsState<DwellerConstructionJobState>.Target(
						i => Agent.Inventory.Value = i,
						() => Agent.Inventory.Value,
						i => Agent.InventoryCapacity.Value.GetCapacityFor(Agent.Inventory.Value, i),
						i => target.Inventory.Value = i,
						() => target.Inventory.Value,
						promise.Inventory,
						Agent.WithdrawalCooldown.Value
					)
				);

				sourceState.step = Steps.WithdrawingItemsFromCache;
			}
		}

		class ToNavigateToWithdrawalItemsFromCache : AgentTransition<DwellerNavigateState<DwellerConstructionJobState>, GameModel, DwellerModel>
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
				if (Agent.InventoryPromise.Value.Operation != InventoryPromise.Operations.ConstructionDeposit) return false;
				if (!Agent.Inventory.Value.Contains(Agent.InventoryPromise.Value.Inventory))
				{
					Debug.LogError("This should not be able to happen!");
					return false;
				}
				
				target = World.Buildings.AllActive.FirstOrDefault(
					m =>
					{
						if (m.Id.Value != Agent.InventoryPromise.Value.TargetId) return false;
						
						return m.Entrances.Value.Any(e => Mathf.Approximately(0f, Vector3.Distance(Agent.Position.Value.NewY(0f), e.Position.NewY(0f))));
					}
				);

				return target != null;
			}

			public override void Transition()
			{
				transferState.SetTarget(
					new DwellerTransferItemsState<DwellerConstructionJobState>.Target(
						i => target.ConstructionInventory.Value = i,
						() => target.ConstructionInventory.Value,
						i => target.ConstructionInventoryCapacity.Value.GetCapacityFor(target.ConstructionInventory.Value, i),
						i => Agent.Inventory.Value = i,
						() => Agent.Inventory.Value,
						Agent.InventoryPromise.Value.Inventory,
						Agent.DepositCooldown.Value,
						() =>
						{
							target.ConstructionInventoryPromised.Value -= Agent.InventoryPromise.Value.Inventory;
							Agent.InventoryPromise.Value = InventoryPromise.Default();
						}
					)
				);
			}
		}

		class ToNavigateToConstructionSite : AgentTransition<DwellerNavigateState<DwellerConstructionJobState>, GameModel, DwellerModel>
		{
			NavMeshPath path;
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation != InventoryPromise.Operations.ConstructionDeposit) return false;

				var target = DwellerUtility.CalculateNearestEntrance(
					Agent.Position.Value,
					out path,
					out _,
					b =>
					{
						if (b.BuildingState.Value != BuildingStates.Constructing) return false;
						return b.Id.Value == Agent.InventoryPromise.Value.TargetId;
					},
					World.Buildings.AllActive
				);

				return target != null;
			}

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(path);
		}
		
		class ToNavigateToSalvageSite : AgentTransition<DwellerNavigateState<DwellerConstructionJobState>, GameModel, DwellerModel>
		{
			NavMeshPath path;
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation != InventoryPromise.Operations.None) return false;
				if (Agent.InventoryCapacity.Value.IsFull(Agent.Inventory.Value)) return false;

				var target = DwellerUtility.CalculateNearestEntrance(
					Agent.Position.Value,
					out path,
					out _,
					salvageSite =>
					{
						if (salvageSite.BuildingState.Value != BuildingStates.Salvaging) return false;
						return !salvageSite.SalvageInventory.Value.IsEmpty;
					},
					World.Buildings.AllActive
				);

				return target != null;
			}

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(path);
		}
	}
}