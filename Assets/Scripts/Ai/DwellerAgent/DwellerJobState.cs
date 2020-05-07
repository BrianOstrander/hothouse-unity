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
	public abstract class DwellerJobState<S> : AgentState<GameModel, DwellerModel>
		where S : DwellerJobState<S>
	{
		public override string Name => Job + "Job";

		public abstract DwellerModel.Jobs Job { get; }

		ToJobOnShiftBegin toJobOnShiftBegin;
		public ToJobOnShiftBegin GetToJobOnShiftBegin => toJobOnShiftBegin ?? (toJobOnShiftBegin = new ToJobOnShiftBegin(this as S));
		
		public override void OnInitialize()
		{
			AddTransitions(
				new ToIdleOnJobUnassigned(this as S),
				new ToIdleOnShiftEnd(this as S)
			);
		}

		public class ToJobOnShiftBegin : AgentTransition<S, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + jobState.Name + ">";

			S jobState;

			public ToJobOnShiftBegin(S jobState) => this.jobState = jobState; 
			
			public override bool IsTriggered() => jobState.Job == Agent.Job.Value && Agent.JobShift.Value.Contains(World.SimulationTime.Value);
		}
		
		protected class ToIdleOnJobUnassigned : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + jobState.Name + ">";
			
			S jobState;

			public ToIdleOnJobUnassigned(S jobState) => this.jobState = jobState; 
			
			public override bool IsTriggered() => jobState.Job != Agent.Job.Value;
		}
		
		protected class ToIdleOnShiftEnd : AgentTransition<DwellerIdleState, GameModel, DwellerModel>
		{
			public override string Name => base.Name + "<" + jobState.Name + ">";
			
			S jobState;

			public ToIdleOnShiftEnd(S jobState) => this.jobState = jobState; 
			
			public override bool IsTriggered() => !Agent.JobShift.Value.Contains(World.SimulationTime.Value);

			public override void Transition()
			{
				switch (Agent.Desire.Value)
				{
					case Desires.Unknown:
					case Desires.None:
						Agent.Desire.Value = EnumExtensions.GetValues(Desires.Unknown, Desires.None).Random();
						break;
				}
			}
		}
		
		protected class ToUnloadItemsToNearestItemCache : AgentTransition<DwellerTransferItemsState<S>, GameModel, DwellerModel>
		{
			Item.Types[] validItems;
			DwellerTransferItemsState<S> transferState;
			BuildingModel target;

			public ToUnloadItemsToNearestItemCache(
				Item.Types[] validItems,
				DwellerTransferItemsState<S> transferState
			)
			{
				this.validItems = validItems;
				this.transferState = transferState;
			}

			public override bool IsTriggered()
			{
				var currentlyValidItems = validItems.Where(i => 0 < Agent.Inventory.Value.GetCurrent(i));

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
				
				foreach (var validItem in validItems) itemsToUnload.Add(validItem, Agent.Inventory.Value[validItem]);
				
				transferState.SetTarget(
					new DwellerTransferItemsState<S>.Target(
						i => target.Inventory.Value = i,
						() => target.Inventory.Value,
						i => Agent.Inventory.Value = i,
						() => Agent.Inventory.Value,
						Inventory.Populate(itemsToUnload),
						Agent.UnloadCooldown.Value
					)
				);
			}
		}
		
		protected class ToNavigateToNearestItemCache : AgentTransition<DwellerNavigateState<S>, GameModel, DwellerModel>
		{
			Item.Types[] validItems;
			Func<Item.Types, bool> isValidToDumpNonFullItem;
			BuildingModel target;
			NavMeshPath targetPath = new NavMeshPath();
			
			public ToNavigateToNearestItemCache(
				Item.Types[] validItems,
				Func<Item.Types, bool> isValidToDumpNonFullItem = null
			)
			{
				this.validItems = validItems;
				this.isValidToDumpNonFullItem = isValidToDumpNonFullItem ?? (itemType => true);
			}

			public override bool IsTriggered()
			{
				var currentlyValidItems = validItems.Where(i => 0 < Agent.Inventory.Value.GetCurrent(i));

				if (currentlyValidItems.None()) return false; // There are zero of any valid items...
				
				if (currentlyValidItems.Where(i => 0 < Agent.Inventory.Value.GetCapacity(i)).Any(i => !isValidToDumpNonFullItem(i)))
				{
					// There are valid non-full items that were blocked from being dumped...
					return false;
				}
				
				// If we get here, that means either all valid items are full, or there are some but we're not being
				// blocked from dumping them...
				
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
		
		protected class ToLoadItemsFromNearestItemDrop : AgentTransition<DwellerTransferItemsState<S>, GameModel, DwellerModel>
		{
			DwellerModel.Jobs[] validJobs;
			Item.Types[] validItems;
			ItemDropModel target;
			DwellerTransferItemsState<S> transferState;

			public ToLoadItemsFromNearestItemDrop(
				DwellerModel.Jobs[] validJobs,
				Item.Types[] validItems,
				DwellerTransferItemsState<S> transferState
			)
			{
				this.validJobs = validJobs;
				this.validItems = validItems;
				this.transferState = transferState;
			}

			public override bool IsTriggered()
			{
				var itemsWithCapacity = validItems.Where(i => 0 < Agent.Inventory.Value.GetCapacity(i));
				if (itemsWithCapacity.None()) return false;
				
				target = World.ItemDrops.AllActive
					.Where(t => validJobs.Contains(t.Job.Value) && itemsWithCapacity.Any(i => t.Inventory.Value.Any(i)))
					.OrderBy(t => Vector3.Distance(Agent.Position.Value, t.Position.Value))
					.FirstOrDefault();
				
				if (target == null) return false;

				return Mathf.Approximately(0f, Vector3.Distance(Agent.Position.Value.NewY(0f), target.Position.Value.NewY(0f)));
			}

			public override void Transition()
			{
				var itemsToLoad = new Dictionary<Item.Types, int>();
				
				foreach (var validItem in validItems) itemsToLoad.Add(validItem, target.Inventory.Value[validItem]);
				
				transferState.SetTarget(
					new DwellerTransferItemsState<S>.Target(
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
		
		protected class ToNavigateToNearestItemDrop : AgentTransition<DwellerNavigateState<S>, GameModel, DwellerModel>
		{
			DwellerModel.Jobs[] validJobs;
			Item.Types[] validItems;
			ItemDropModel target;
			NavMeshPath targetPath = new NavMeshPath();

			public ToNavigateToNearestItemDrop(
				DwellerModel.Jobs[] validJobs,
				Item.Types[] validItems
			)
			{
				this.validJobs = validJobs;
				this.validItems = validItems;
			}

			public override bool IsTriggered()
			{
				var itemsWithCapacity = validItems.Where(i => 0 < Agent.Inventory.Value.GetCapacity(i));
				if (itemsWithCapacity.None()) return false;

				target = World.ItemDrops.AllActive
					.Where(t => validJobs.Contains(t.Job.Value) && itemsWithCapacity.Any(i => t.Inventory.Value.Any(i)))
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