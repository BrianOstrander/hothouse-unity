using System;
using System.Linq;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class ItemCleanupState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		const int CleanupCountTimeout = 1;
		
		public override string Name => "ItemCleanup";

		Jobs[] validJobs;
		Inventory.Types[] validItems;

		int cleanupCount;

		public void ResetCleanupCount(Inventory.Types[] validItems)
		{
			this.validItems = validItems; 
			cleanupCount = validItems?.Length ?? CleanupCountTimeout;
		}

		public ItemCleanupState(Jobs[] validJobs)
		{
			this.validJobs = validJobs;
		}

		public override void OnInitialize()
		{
			var transferItemsState = new TransferItemsState<ItemCleanupState<S>>();
			
			AddChildStates(
				transferItemsState,
				new NavigateState<ItemCleanupState<S>>()
			);
			
			AddTransitions(
				new AgentTransitionFallthrough<S,GameModel,DwellerModel>(
					"CleanupTimeout",
					() => cleanupCount <= 0,
					() => validItems = null
				),
				new ToDepositItemsInNearestBuilding(transferItemsState),
				new ToWithdrawalItemsFromNearestItemDrop(transferItemsState),
				new ToNavigateToNearestItemDrop(),
				new ToNavigateToNearestBuilding()
			);
		}

		public override void Begin()
		{
			if (validItems == null) throw new NullReferenceException(nameof(validItems) + " cannot be null, did you forget to call " + nameof(ResetCleanupCount) + "?");
			if (validItems.None()) Debug.LogWarning("Transitioned to " + Name + " with an empty list of items, nothing will be cleaned up");
		}

		public override void Idle() => cleanupCount--;

		class ToDepositItemsInNearestBuilding : AgentTransition<ItemCleanupState<S>, TransferItemsState<ItemCleanupState<S>>, GameModel, DwellerModel>
		{
			TransferItemsState<ItemCleanupState<S>> transferState;
			BuildingModel target;

			public ToDepositItemsInNearestBuilding(TransferItemsState<ItemCleanupState<S>> transferState)
			{
				this.transferState = transferState;
			}

			public override bool IsTriggered()
			{
				var currentlyValidItems = SourceState.validItems.Where(i => 0 < Agent.Inventory.All.Value[i]);

				if (currentlyValidItems.None()) return false; // There are zero of any valid items...
				
				target = NavigationUtility.CalculateNearestAvailableOperatingEntrance(
					Agent.Transform.Position.Value,
					out _,
					out var entrancePosition,
					b =>
					{
						if (!b.Inventory.Permission.Value.CanDeposit(Agent.Job.Value)) return false;
						return currentlyValidItems.Any(i => b.Inventory.AvailableCapacity.Value.HasCapacityFor(b.Inventory.Available.Value, i));
					},
					Game.Buildings.AllActive
				);

				if (target == null) return false;

				return Mathf.Approximately(0f, Vector3.Distance(Agent.Transform.Position.Value.NewY(0f), entrancePosition.NewY(0f)));
			}

			public override void Transition()
			{
				var itemsToTransfer = new Dictionary<Inventory.Types, int>();
				
				foreach (var validItem in SourceState.validItems) itemsToTransfer.Add(validItem, Agent.Inventory.All.Value[validItem]);
				
				transferState.SetTarget(
					new TransferItemsState<ItemCleanupState<S>>.Target(
						i => target.Inventory.Add(i),
						() => target.Inventory.Available.Value,
						i => target.Inventory.AvailableCapacity.Value.GetCapacityFor(target.Inventory.Available.Value, i),
						i => Agent.Inventory.Remove(i),
						() => Agent.Inventory.All.Value,
						new Inventory(itemsToTransfer),
						Agent.DepositCooldown.Value
					)
				);

				SourceState.cleanupCount--;
			}
		}
		
		class ToNavigateToNearestBuilding : AgentTransition<ItemCleanupState<S>, NavigateState<ItemCleanupState<S>>, GameModel, DwellerModel>
		{
			BuildingModel target;
			NavMeshPath targetPath = new NavMeshPath();
			
			public override bool IsTriggered()
			{
				var currentlyValidItems = SourceState.validItems.Where(i => 0 < Agent.Inventory.All.Value[i]);

				if (currentlyValidItems.None()) return false; // There are zero of any valid items...
				
				target = NavigationUtility.CalculateNearestAvailableOperatingEntrance(
					Agent.Transform.Position.Value,
					out targetPath,
					out _,
					b =>
					{
						if (!b.Inventory.Permission.Value.CanDeposit(Agent.Job.Value)) return false;
						return currentlyValidItems.Any(i => b.Inventory.AvailableCapacity.Value.HasCapacityFor(b.Inventory.Available.Value, i));
					},
					Game.Buildings.AllActive
				);

				return target != null;
			}

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(targetPath);
			}
		}
		
		class ToWithdrawalItemsFromNearestItemDrop : AgentTransition<ItemCleanupState<S>, TransferItemsState<ItemCleanupState<S>>, GameModel, DwellerModel>
		{
			TransferItemsState<ItemCleanupState<S>> transferState;
			IInventoryModel source;

			public ToWithdrawalItemsFromNearestItemDrop(TransferItemsState<ItemCleanupState<S>> transferState)
			{
				this.transferState = transferState;
			}

			public override bool IsTriggered()
			{
				if (Agent.Inventory.IsFull()) return false;
				if (Agent.InventoryPromise.Value.Operation != InventoryPromiseOld.Operations.CleanupWithdrawal) return false;

				if (Agent.InventoryPromise.Value.Source.TryGetInstance(Game, out source))
				{
					return Mathf.Approximately(0f, Vector3.Distance(Agent.Transform.Position.Value.NewY(0f), source.Transform.Position.Value.NewY(0f)));
				}
				
				// It might happen if the item drop is destroyed...
				Debug.LogError("Unable to find an active model of type \""+nameof(ItemDropModel)+"\" with id \""+Agent.InventoryPromise.Value.Target.Id+"\", this should never happen");
				return false;
			}

			public override void Transition()
			{
				transferState.SetTarget(
					new TransferItemsState<ItemCleanupState<S>>.Target(
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
						Agent.WithdrawalCooldown.Value,
						() =>
						{
							Debug.Log("don???");
							Agent.InventoryPromise.Value = InventoryPromiseOld.Default();
						})
				);
			}
		}
		
		class ToNavigateToNearestItemDrop : AgentTransition<ItemCleanupState<S>, NavigateState<ItemCleanupState<S>>, GameModel, DwellerModel>
		{
			NavMeshPath targetPath = new NavMeshPath();
			InventoryPromiseOld promise;
			Inventory inventoryToWithdrawal;
			ItemDropModel source;

			public override bool IsTriggered()
			{
				if (Agent.Inventory.IsFull()) return false;

				return NavigationUtility.CalculateNearestCleanupWithdrawal(
					Agent,
					Game,
					SourceState.validItems,
					SourceState.validJobs,
					out targetPath,
					out promise,
					out inventoryToWithdrawal,
					out source
				);
			}

			public override void Transition()
			{
				source.Inventory.AddForbidden(inventoryToWithdrawal);

				Agent.InventoryPromise.Value = promise;
				
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(targetPath);
			}
		}
	}
}