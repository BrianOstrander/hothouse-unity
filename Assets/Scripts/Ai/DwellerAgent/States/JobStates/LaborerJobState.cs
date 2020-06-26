using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class LaborerJobState : JobState<LaborerJobState>
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
						if (!possibleConstructionSite.IsBuildingState(BuildingStates.Constructing)) return false;
						if (possibleConstructionSite.LightSensitive.IsNotLit) return false;

						return possibleConstructionSite.ConstructionInventoryCapacity.Value.IsNotFull(possibleConstructionSite.ConstructionInventory.Value + possibleConstructionSite.ConstructionInventoryPromised.Value);
					}
				)
				.ToDictionary(b => b, b => false);

			var itemSourceResult = NavigationUtility.CalculateNearestAvailableOperatingEntrance(
				agent.Transform.Position.Value,
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
							var navigationValid = NavigationUtility.CalculateNearestEntrance(
								agent.Transform.Position.Value,
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
		
		public override Jobs Job => Jobs.Laborer;

		Steps step;
		
		public override void OnInitialize()
		{
			var validJobs = new[] { Jobs.Laborer, Jobs.None };
			var validCleanupItems = Inventory.ValidTypes;
			
			var transferItemsState = new TransferItemsState<LaborerJobState>();
			var timeoutState = new TimeoutState<LaborerJobState>();
			
			var cleanupState = new ItemCleanupState<LaborerJobState>(
				validJobs
			);
			
			AddChildStates(
				transferItemsState,
				timeoutState,
				cleanupState,
				new NavigateState<LaborerJobState>(),
				new ObligationState<LaborerJobState>()
			);
			
			AddTransitions(
				new ToIdleOnJobUnassigned(this),
			
				new ToWithdrawalItemsFromCache(transferItemsState),
				new ToDepositToNearestConstructionSite(transferItemsState),
				new ToWithdrawalItemsFromSalvageSite(transferItemsState),
				new ToNavigateToSalvageSite(),
				new ToNavigateToConstructionSite(),

				new ToObligationOnObligationAvailable(),
				
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
				new DropItemsTransition<LaborerJobState>(timeoutState),
				new NavigateToNearestLightTransition<LaborerJobState>()
			);
		}

		public override void Begin()
		{
			switch (step)
			{
				case Steps.Unknown:
					return;
			}
			
			var constructionSite = Game.Buildings.AllActive.FirstOrDefault(b => b.Id.Value == Agent.InventoryPromise.Value.TargetId);
			
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
		
		class ToWithdrawalItemsFromSalvageSite : AgentTransition<TransferItemsState<LaborerJobState>, GameModel, DwellerModel>
		{
			TransferItemsState<LaborerJobState> transferState;
			BuildingModel target;
			Inventory itemsToLoad;
			
			public ToWithdrawalItemsFromSalvageSite(TransferItemsState<LaborerJobState> transferState)
			{
				this.transferState = transferState;
			}
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation != InventoryPromise.Operations.None) return false;
				if (Agent.InventoryCapacity.Value.IsFull(Agent.Inventory.Value)) return false;
				
				target = NavigationUtility.CalculateNearestAvailableEntrance(
					Agent.Transform.Position.Value,
					out _,
					out var entrancePosition,
					salvageSite =>
					{
						if (salvageSite.BuildingState.Value != BuildingStates.Salvaging) return false;
						if (salvageSite.SalvageInventory.Value.IsEmpty) return false;
						itemsToLoad = salvageSite.SalvageInventory.Value;
						return true;
					},
					Game.Buildings.AllActive
				);

				if (target == null) return false;
				
				return Vector3.Distance(Agent.Transform.Position.Value.NewY(0f), entrancePosition.NewY(0f)) < Agent.TransferDistance.Value;
			}

			public override void Transition()
			{
				transferState.SetTarget(
					new TransferItemsState<LaborerJobState>.Target(
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

		class ToWithdrawalItemsFromCache : AgentTransition<LaborerJobState, TransferItemsState<LaborerJobState>, GameModel, DwellerModel>
		{
			TransferItemsState<LaborerJobState> transferState;
			BuildingModel target;
			InventoryPromise promise;

			public ToWithdrawalItemsFromCache(TransferItemsState<LaborerJobState> transferState)
			{
				this.transferState = transferState;
			}
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation != InventoryPromise.Operations.None) return false;

				target = GetNearestItemSource(
					Game,
					Agent,
					out _,
					out var entrancePosition,
					out promise
				);

				if (target == null) return false;
				
				return Vector3.Distance(Agent.Transform.Position.Value.NewY(0f), entrancePosition.NewY(0f)) < Agent.TransferDistance.Value;
			}

			public override void Transition()
			{
				var constructionSite = Game.Buildings.AllActive.First(b => b.Id.Value == promise.TargetId);
				
				Agent.InventoryPromise.Value = promise;
				constructionSite.ConstructionInventoryPromised.Value += promise.Inventory;

				transferState.SetTarget(
					new TransferItemsState<LaborerJobState>.Target(
						i => Agent.Inventory.Value = i,
						() => Agent.Inventory.Value,
						i => Agent.InventoryCapacity.Value.GetCapacityFor(Agent.Inventory.Value, i),
						i => target.Inventory.Value = i,
						() => target.Inventory.Value,
						promise.Inventory,
						Agent.WithdrawalCooldown.Value
					)
				);

				SourceState.step = Steps.WithdrawingItemsFromCache;
			}
		}

		class ToNavigateToWithdrawalItemsFromCache : AgentTransition<NavigateState<LaborerJobState>, GameModel, DwellerModel>
		{
			NavMeshPath path;
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation != InventoryPromise.Operations.None) return false;

				var target = GetNearestItemSource(
					Game,
					Agent,
					out path,
					out _,
					out _
				);

				return target != null;
			}

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(path);
		}

		class ToDepositToNearestConstructionSite : AgentTransition<TransferItemsState<LaborerJobState>, GameModel, DwellerModel>
		{
			TransferItemsState<LaborerJobState> transferState;
			BuildingModel target;

			public ToDepositToNearestConstructionSite(TransferItemsState<LaborerJobState> transferState)
			{
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
				
				target = Game.Buildings.AllActive.FirstOrDefault(
					m =>
					{
						if (m.Id.Value != Agent.InventoryPromise.Value.TargetId) return false;
						
						return m.Enterable.Entrances.Value.Any(e => Vector3.Distance(Agent.Transform.Position.Value.NewY(0f), e.Position.NewY(0f)) < Agent.TransferDistance.Value);
					}
				);

				return target != null;
			}

			public override void Transition()
			{
				transferState.SetTarget(
					new TransferItemsState<LaborerJobState>.Target(
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

		class ToNavigateToConstructionSite : AgentTransition<NavigateState<LaborerJobState>, GameModel, DwellerModel>
		{
			NavMeshPath path;
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation != InventoryPromise.Operations.ConstructionDeposit) return false;

				var target = NavigationUtility.CalculateNearestAvailableEntrance(
					Agent.Transform.Position.Value,
					out path,
					out _,
					b =>
					{
						if (b.BuildingState.Value != BuildingStates.Constructing) return false;
						return b.Id.Value == Agent.InventoryPromise.Value.TargetId;
					},
					Game.Buildings.AllActive
				);

				return target != null;
			}

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(path);
		}
		
		class ToNavigateToSalvageSite : AgentTransition<NavigateState<LaborerJobState>, GameModel, DwellerModel>
		{
			NavMeshPath path;
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation != InventoryPromise.Operations.None) return false;
				if (Agent.InventoryCapacity.Value.IsFull(Agent.Inventory.Value)) return false;

				var target = NavigationUtility.CalculateNearestAvailableEntrance(
					Agent.Transform.Position.Value,
					out path,
					out _,
					salvageSite =>
					{
						if (salvageSite.BuildingState.Value != BuildingStates.Salvaging) return false;
						return !salvageSite.SalvageInventory.Value.IsEmpty;
					},
					Game.Buildings.AllActive
				);

				return target != null;
			}

			public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(path);
		}
	}
}