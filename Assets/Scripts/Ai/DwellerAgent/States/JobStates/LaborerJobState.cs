using System;
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

						var nonReserved = possibleConstructionSite.ConstructionInventory.AllCapacity.Value.GetMaximum() - possibleConstructionSite.ConstructionInventory.ReservedCapacity.Value.GetMaximum();
						
						if (nonReserved.IsEmpty) return false;

						return !(nonReserved - possibleConstructionSite.ConstructionInventory.All.Value).IsEmpty;
					}
				)
				.ToDictionary(b => b, b => false);

			var itemSourceResult = NavigationUtility.CalculateNearestAvailableOperatingEntrance(
				agent.Transform.Position.Value,
				out path,
				out entrancePosition,
				possibleItemSource =>
				{
					if (possibleItemSource.Inventory.Available.Value.IsEmpty) return false;
					if (!possibleItemSource.Inventory.Permission.Value.CanWithdrawal(agent.Job.Value)) return false;

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

						var nonPromisedInventory = kv.Key.ConstructionInventory.AvailableCapacity.Value.GetCapacityFor(
							kv.Key.ConstructionInventory.All.Value + kv.Key.ConstructionInventory.ReservedCapacity.Value.GetMaximum()
						);
						
						if (nonPromisedInventory.Intersects(possibleItemSource.Inventory.Available.Value, out var intersection))
						{
							agent.Inventory.Capacity.Value.GetClamped(
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

			switch (Agent.InventoryPromise.Value.Operation)
			{
				case InventoryPromise.Operations.None:
					break;
				case InventoryPromise.Operations.ConstructionDeposit:
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
							if (!Agent.Inventory.All.Value.Contains(Agent.InventoryPromise.Value.Inventory))
							{
								// The dweller was unable to pull all the resources it wanted to, so we're going to correct the
								// amount we promised
								Agent.InventoryPromise.Value.Inventory.Intersects(
									Agent.Inventory.All.Value,
									out var newPromise
								);
								// constructionSite.ConstructionInventoryPromised.Value -= Agent.InventoryPromise.Value.Inventory - newPromise;
								constructionSite.ConstructionInventory.RemoveReserved(Agent.InventoryPromise.Value.Inventory - newPromise);

								Agent.InventoryPromise.Value = Agent.InventoryPromise.Value.NewInventory(newPromise);
							}
							break;
					}
			
					step = Steps.Unknown;
					
					break;
				case InventoryPromise.Operations.CleanupWithdrawal:
					Agent.InventoryPromise.Value = InventoryPromise.Default();
					break;
				default:
					Debug.LogError("Unrecognized InventoryPromise.Operation: "+Agent.InventoryPromise.Value.Operation);
					break;
			}
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
				if (Agent.Inventory.IsFull()) return false;
				
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
						i => Agent.Inventory.Add(i),
						() => Agent.Inventory.All.Value,
						i => Agent.Inventory.Capacity.Value.GetCapacityFor(Agent.Inventory.All.Value, i),
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
			IInventoryModel source;

			public ToWithdrawalItemsFromCache(TransferItemsState<LaborerJobState> transferState)
			{
				this.transferState = transferState;
			}
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation == InventoryPromise.Operations.None) return false;
				if (Agent.Inventory.All.Value.Contains(Agent.InventoryPromise.Value.Inventory)) return false;

				if (!Agent.InventoryPromise.Value.Source.TryGetInstance(Game, out source))
				{
					// Source must have been destroyed...
					Debug.LogError("Target for inventory promise was destroyed, but the inventory promise was not reset? This should not occur\n"+Agent.InventoryPromise.Value);
					return false;
				}

				return source.Enterable.Entrances.Value
					.Any(e => e.State == Entrance.States.Available && Vector3.Distance(Agent.Transform.Position.Value.NewY(0f), e.Position.NewY(0f)) < Agent.TransferDistance.Value);
			}

			public override void Transition()
			{
				transferState.SetTarget(
					new TransferItemsState<LaborerJobState>.Target(
						i => Agent.Inventory.Add(i),
						() => Agent.Inventory.All.Value,
						i => Agent.Inventory.Capacity.Value.GetCapacityFor(Agent.Inventory.All.Value, i),
						i =>
						{
							source.Inventory.RemoveForbidden(i);
							source.Inventory.Remove(i);
						},
						() => source.Inventory.All.Value,
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
			IInventoryModel source;
			InventoryPromise promise;
			
			public override bool IsTriggered()
			{
				if (Agent.InventoryPromise.Value.Operation != InventoryPromise.Operations.None) return false;

				source = GetNearestItemSource(
					Game,
					Agent,
					out path,
					out _,
					out promise
				);

				return source != null;
			}

			public override void Transition()
			{
				promise.Target.TryGetInstance<IConstructionModel>(Game, out var constructionSite);
			
				source.Inventory.AddForbidden(promise.Inventory);
				constructionSite.ConstructionInventory.AddReserved(promise.Inventory);

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
				if (!Agent.Inventory.All.Value.Contains(Agent.InventoryPromise.Value.Inventory)) return false;

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
						i => target.ConstructionInventory.RemoveReserved(i).Add(i),
						() => target.ConstructionInventory.All.Value,
						i => target.ConstructionInventory.AllCapacity.Value.GetCapacityFor(target.ConstructionInventory.All.Value, i),
						i => Agent.Inventory.Remove(i),
						() => Agent.Inventory.All.Value,
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
				if (Agent.Inventory.IsFull()) return false;

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