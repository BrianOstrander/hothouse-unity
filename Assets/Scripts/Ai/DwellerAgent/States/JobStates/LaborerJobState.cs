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

						var nonReserved = possibleConstructionSite.ConstructionInventoryzzz.AllCapacity.Value.GetMaximum() - possibleConstructionSite.ConstructionInventoryzzz.ReservedCapacity.Value.GetMaximum();
						
						if (nonReserved.IsEmpty) return false;

						return !(nonReserved - possibleConstructionSite.ConstructionInventoryzzz.All.Value).IsEmpty;
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

						var nonPromisedInventory = kv.Key.ConstructionInventoryzzz.AvailableCapacity.Value.GetCapacityFor(
							kv.Key.ConstructionInventoryzzz.All.Value + kv.Key.ConstructionInventoryzzz.ReservedCapacity.Value.GetMaximum()
						);
						
						if (nonPromisedInventory.Intersects(possibleItemSource.Inventory.Value, out var intersection))
						{
							agent.InventoryCapacity.Value.GetClamped(
								intersection,
								out var promisedInventory
							);
							
							promiseResult = new InventoryPromise(
								InstanceId.New(possibleItemSource), 
								InstanceId.New(kv.Key),
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
			
			if (!Agent.InventoryPromise.Value.Target.TryGetInstance<IConstructionModel>(Game, out var constructionSite))
			{
				// Building must have been destroyed...
				Agent.InventoryPromise.Value = InventoryPromise.Default();
				step = Steps.Unknown;
				Debug.LogError("Need to check that we're not on the way to navigating to the item cache! must unforbid anything!");
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
						// constructionSite.ConstructionInventoryPromised.Value -= Agent.InventoryPromise.Value.Inventory - newPromise;
						constructionSite.ConstructionInventoryzzz.RemoveReserved(Agent.InventoryPromise.Value.Inventory - newPromise);

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
						i => Agent.Inventory.Value += i,
						() => Agent.Inventory.Value,
						i => Agent.InventoryCapacity.Value.GetCapacityFor(Agent.Inventory.Value, i),
						i => target.SalvageInventory.Value -= i,
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
			BuildingModel source;

			public ToWithdrawalItemsFromCache(TransferItemsState<LaborerJobState> transferState)
			{
				this.transferState = transferState;
			}
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation == InventoryPromise.Operations.None) return false;
				if (Agent.Inventory.Value.Contains(Agent.InventoryPromise.Value.Inventory)) return false;

				if (!Agent.InventoryPromise.Value.Source.TryGetInstance(Game, out source))
				{
					// Source must have been destroyed...
					Debug.LogError("Target for inventory promise was destroyed, but the inventory promise was not reset? This should not occur\n"+Agent.InventoryPromise.Value);
					return false;
				}

				return source.Enterable.Entrances.Value
					.Any(e => e.State == Entrance.States.Available && Vector3.Distance(Agent.Transform.Position.Value.NewY(0f), e.Position.NewY(0f)) < Agent.TransferDistance.Value);
				
				// target = GetNearestItemSource(
				// 	Game,
				// 	Agent,
				// 	out _,
				// 	out var entrancePosition,
				// 	out promise
				// );
				//
				// if (target == null) return false;
				//
				// return Vector3.Distance(Agent.Transform.Position.Value.NewY(0f), entrancePosition.NewY(0f)) < Agent.TransferDistance.Value;
			}

			public override void Transition()
			{
				Debug.Log("Unforbid items here");

				transferState.SetTarget(
					new TransferItemsState<LaborerJobState>.Target(
						i => Agent.Inventory.Value += i,
						() => Agent.Inventory.Value,
						i => Agent.InventoryCapacity.Value.GetCapacityFor(Agent.Inventory.Value, i),
						i => source.Inventory.Value -= i,
						() => source.Inventory.Value,
						Agent.InventoryPromise.Value.Inventory,
						Agent.WithdrawalCooldown.Value
					)
				);

				SourceState.step = Steps.WithdrawingItemsFromCache;
			}
		}

		class ToNavigateToWithdrawalItemsFromCache : AgentTransition<NavigateState<LaborerJobState>, GameModel, DwellerModel>
		{
			NavMeshPath path;
			InventoryPromise promise;
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation != InventoryPromise.Operations.None) return false;

				var target = GetNearestItemSource(
					Game,
					Agent,
					out path,
					out _,
					out promise
				);

				return target != null;
			}

			public override void Transition()
			{
				Debug.Break();
				
				promise.Target.TryGetInstance<IConstructionModel>(Game, out var constructionSite);
				
				Debug.Log("Forbid items here");
				constructionSite.ConstructionInventoryzzz.AddReserved(promise.Inventory);

				Agent.InventoryPromise.Value = promise;
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(path);
			}
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
				if (!Agent.Inventory.Value.Contains(Agent.InventoryPromise.Value.Inventory)) return false;

				if (Agent.InventoryPromise.Value.Target.TryGetInstance(Game, out target))
				{
					return target.Enterable.Entrances.Value.Any(e => Vector3.Distance(Agent.Transform.Position.Value.NewY(0f), e.Position.NewY(0f)) < Agent.TransferDistance.Value);
				}

				return false;
			}

			public override void Transition()
			{
				transferState.SetTarget(
					// new TransferItemsState<LaborerJobState>.Target(
					// 	i => target.ConstructionInventory.Value = i,
					// 	() => target.ConstructionInventory.Value,
					// 	i => target.ConstructionInventoryCapacity.Value.GetCapacityFor(target.ConstructionInventory.Value, i),
					// 	i => Agent.Inventory.Value = i,
					// 	() => Agent.Inventory.Value,
					// 	Agent.InventoryPromise.Value.Inventory,
					// 	Agent.DepositCooldown.Value,
					// 	() =>
					// 	{
					// 		target.ConstructionInventoryPromised.Value -= Agent.InventoryPromise.Value.Inventory;
					// 		Agent.InventoryPromise.Value = InventoryPromise.Default();
					// 	}
					// )
					new TransferItemsState<LaborerJobState>.Target(
						i => target.ConstructionInventoryzzz.RemoveReserved(i).Add(i),
						() => target.ConstructionInventoryzzz.All.Value,
						i => target.ConstructionInventoryzzz.AllCapacity.Value.GetCapacityFor(target.ConstructionInventoryzzz.All.Value, i),
						i => Agent.Inventory.Value -= i,
						() => Agent.Inventory.Value,
						Agent.InventoryPromise.Value.Inventory,
						Agent.DepositCooldown.Value,
						() =>
						{
							// target.ConstructionInventoryPromised.Value -= Agent.InventoryPromise.Value.Inventory;
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

				if (!Agent.InventoryPromise.Value.Target.TryGetInstance<IConstructionModel>(Game, out var target)) return false;

				var found = NavigationUtility.CalculateNearest(
					Agent.Transform.Position.Value,
					out var result,
					Navigation.QueryEntrances(target)
				);

				if (!found) return false;

				path = result.Path;
				return true;
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