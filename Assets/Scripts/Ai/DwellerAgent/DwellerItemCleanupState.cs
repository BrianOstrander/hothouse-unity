using System;
using System.Linq;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerItemCleanupState<S> : AgentState<GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		const int UnloadCountTimeout = 1;
		
		public override string Name => "ItemCleanup";

		DwellerModel.Jobs[] validJobs;
		Item.Types[] validItems;

		int unloadCount;

		public void ResetUnloadCount() => unloadCount = UnloadCountTimeout;
		
		public DwellerItemCleanupState(
			DwellerModel.Jobs[] validJobs,
			Item.Types[] validItems
		)
		{
			this.validJobs = validJobs;
			this.validItems = validItems;

			ResetUnloadCount();
		}

		public override void OnInitialize()
		{
			var transferItemsState = new DwellerTransferItemsState<DwellerItemCleanupState<S>>();
			
			AddChildStates(
				transferItemsState,
				new DwellerNavigateState<DwellerItemCleanupState<S>>()
			);
			
			AddTransitions(
				new AgentTransitionFallthrough<S,GameModel,DwellerModel>(
					"UnloadTimeout",
					() => unloadCount <= 0
				),
				new ToUnloadItemsToNearestItemCache(
					this,	
					transferItemsState
				),
				new ToLoadItemsFromNearestItemDrop(
					this,
					transferItemsState
				),
				new ToNavigateToNearestItemDrop(this),
				new ToNavigateToNearestItemCache(this)
			);
		}

		public override void Idle() => unloadCount--;

		class ToUnloadItemsToNearestItemCache : AgentTransition<DwellerTransferItemsState<DwellerItemCleanupState<S>>, GameModel, DwellerModel>
		{
			DwellerItemCleanupState<S> sourceState;
			DwellerTransferItemsState<DwellerItemCleanupState<S>> transferState;
			BuildingModel target;

			public ToUnloadItemsToNearestItemCache(
				DwellerItemCleanupState<S> sourceState,
				DwellerTransferItemsState<DwellerItemCleanupState<S>> transferState
			)
			{
				this.sourceState = sourceState;
				this.transferState = transferState;
			}

			public override bool IsTriggered()
			{
				var currentlyValidItems = sourceState.validItems.Where(i => 0 < Agent.Inventory.Value.GetCurrent(i));

				if (currentlyValidItems.None()) return false; // There are zero of any valid items...
				
				target = DwellerUtility.CalculateNearestEntrance(
					Agent.Position.Value,
					World.Buildings.AllActive,
					b => currentlyValidItems.Any(i => 0 < b.Inventory.Value.GetCapacity(i)),
					out _,
					out var entrancePosition
				);

				if (target == null) return false;

				return Mathf.Approximately(0f, Vector3.Distance(Agent.Position.Value.NewY(0f), entrancePosition.NewY(0f)));
			}

			public override void Transition()
			{
				var itemsToUnload = new Dictionary<Item.Types, int>();
				
				foreach (var validItem in sourceState.validItems) itemsToUnload.Add(validItem, Agent.Inventory.Value[validItem]);
				
				transferState.SetTarget(
					new DwellerTransferItemsState<DwellerItemCleanupState<S>>.Target(
						i => target.Inventory.Value = i,
						() => target.Inventory.Value,
						i => Agent.Inventory.Value = i,
						() => Agent.Inventory.Value,
						Inventory.Populate(itemsToUnload),
						Agent.UnloadCooldown.Value
					)
				);

				sourceState.unloadCount--;
			}
		}
		
		class ToNavigateToNearestItemCache : AgentTransition<DwellerNavigateState<DwellerItemCleanupState<S>>, GameModel, DwellerModel>
		{
			DwellerItemCleanupState<S> sourceState;
			BuildingModel target;
			NavMeshPath targetPath = new NavMeshPath();
			
			public ToNavigateToNearestItemCache(DwellerItemCleanupState<S> sourceState) => this.sourceState = sourceState;

			public override bool IsTriggered()
			{
				var currentlyValidItems = sourceState.validItems.Where(i => 0 < Agent.Inventory.Value.GetCurrent(i));

				if (currentlyValidItems.None()) return false; // There are zero of any valid items...
				
				// If we get here, that means either all valid items are full, or there are some but we're not being
				// blocked from dumping them... OUT OF DATE DESC
				
				target = DwellerUtility.CalculateNearestEntrance(
					Agent.Position.Value,
					World.Buildings.AllActive,
					b => currentlyValidItems.Any(i => 0 < b.Inventory.Value.GetCapacity(i)),
					out targetPath,
					out _
				);

				return target != null;
			}

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(targetPath);
			}
		}
		
		class ToLoadItemsFromNearestItemDrop : AgentTransition<DwellerTransferItemsState<DwellerItemCleanupState<S>>, GameModel, DwellerModel>
		{
			DwellerItemCleanupState<S> sourceState;
			ItemDropModel target;
			DwellerTransferItemsState<DwellerItemCleanupState<S>> transferState;

			public ToLoadItemsFromNearestItemDrop(
				DwellerItemCleanupState<S> sourceState,
				DwellerTransferItemsState<DwellerItemCleanupState<S>> transferState
			)
			{
				this.sourceState = sourceState;
				this.transferState = transferState;
			}

			public override bool IsTriggered()
			{
				var itemsWithCapacity = sourceState.validItems.Where(i => 0 < Agent.Inventory.Value.GetCapacity(i));
				if (itemsWithCapacity.None()) return false;
				
				target = World.ItemDrops.AllActive
					.Where(t => sourceState.validJobs.Contains(t.Job.Value) && itemsWithCapacity.Any(i => t.Inventory.Value.Any(i)))
					.OrderBy(t => Vector3.Distance(Agent.Position.Value, t.Position.Value))
					.FirstOrDefault();
				
				if (target == null) return false;

				return Mathf.Approximately(0f, Vector3.Distance(Agent.Position.Value.NewY(0f), target.Position.Value.NewY(0f)));
			}

			public override void Transition()
			{
				var itemsToLoad = new Dictionary<Item.Types, int>();
				
				foreach (var validItem in sourceState.validItems) itemsToLoad.Add(validItem, target.Inventory.Value[validItem]);
				
				transferState.SetTarget(
					new DwellerTransferItemsState<DwellerItemCleanupState<S>>.Target(
						i => Agent.Inventory.Value = i,
						() => Agent.Inventory.Value,
						i => target.Inventory.Value = i,
						() => target.Inventory.Value,
						Inventory.Populate(itemsToLoad),
						Agent.LoadCooldown.Value
					)
				);
			}
		}
		
		class ToNavigateToNearestItemDrop : AgentTransition<DwellerNavigateState<DwellerItemCleanupState<S>>, GameModel, DwellerModel>
		{
			DwellerItemCleanupState<S> sourceState;
			ItemDropModel target;
			NavMeshPath targetPath = new NavMeshPath();

			public ToNavigateToNearestItemDrop(DwellerItemCleanupState<S> sourceState) => this.sourceState = sourceState;

			public override bool IsTriggered()
			{
				var itemsWithCapacity = sourceState.validItems.Where(i => 0 < Agent.Inventory.Value.GetCapacity(i));
				if (itemsWithCapacity.None()) return false;

				target = World.ItemDrops.AllActive
					.Where(t => sourceState.validJobs.Contains(t.Job.Value) && itemsWithCapacity.Any(i => t.Inventory.Value.Any(i)))
					.OrderBy(t => Vector3.Distance(Agent.Position.Value, t.Position.Value))
					.FirstOrDefault(
						t =>  NavMesh.CalculatePath(
							Agent.Position.Value,
							t.Position.Value,
							NavMesh.AllAreas,
							targetPath
						)
					);

				return target != null;
			}

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(targetPath);
			}
		}
	}
}