using System;
using System.Linq;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai
{
	public abstract class DwellerJobState<S> : AgentState<GameModel, DwellerModel>
		where S : DwellerJobState<S>
	{
		public override string Name => Job + "Job";

		public abstract Jobs Job { get; }

		ToJobOnShiftBegin toJobOnShiftBegin;
		public ToJobOnShiftBegin GetToJobOnShiftBegin => toJobOnShiftBegin ?? (toJobOnShiftBegin = new ToJobOnShiftBegin(this as S));
		
		public override void OnInitialize()
		{
			// TODO: Should I be casting to S? I think I should just be using DwellerJobState<S> in transitions...
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

		protected class ToItemCleanupOnValidInventory : AgentTransition<DwellerItemCleanupState<S>, GameModel, DwellerModel>
		{
			public enum InventoryTrigger
			{
				Unknown = 0,
				Any = 10,
				None = 20,
				NonZeroMaximumFull = 40,
				SomeOrNonZeroMaximumFull = 50
			}

			public override string Name => base.Name + "." + inventoryTrigger;

			DwellerItemCleanupState<S> cleanupState;
			InventoryTrigger inventoryTrigger;
			Jobs[] validJobs;
			Item.Types[] validItems;
			
			NavMeshPath targetPath = new NavMeshPath();

			public ToItemCleanupOnValidInventory(
				DwellerItemCleanupState<S> cleanupState,
				InventoryTrigger inventoryTrigger,
				Jobs[] validJobs,
				Item.Types[] validItems
			)
			{
				this.cleanupState = cleanupState;
				this.inventoryTrigger = inventoryTrigger;
				this.validJobs = validJobs;
				this.validItems = validItems;
			}

			public override bool IsTriggered()
			{
				if (inventoryTrigger == InventoryTrigger.Any) return true;

				switch (inventoryTrigger)
				{
					case InventoryTrigger.Any:
						return true;
					case InventoryTrigger.None:
						if (validItems.Any(i => Agent.Inventory.Value.Any(i))) return false;
						return IsAnyValidItemReachable();
					case InventoryTrigger.NonZeroMaximumFull:
						if (0 < Agent.Inventory.Value.GetSharedMinimumCapacity(validItems)) return false;
						// if (validItems.None(i => Agent.Inventory.Value.IsNonZeroMaximumFull(i))) return false;
						return IsAnyBuildingWithInventoryReachable();
					case InventoryTrigger.SomeOrNonZeroMaximumFull:
						var someValidItems = validItems.Where(i => Agent.Inventory.Value.Any(i));
						if (someValidItems.None()) return false;
						return IsAnyValidItemReachable() || IsAnyBuildingWithInventoryReachable();
				}
				
				Debug.LogError("Unrecognized "+nameof(inventoryTrigger)+": "+inventoryTrigger);
				return false;
			}

			bool IsAnyValidItemReachable()
			{
				var itemsWithCapacity = validItems.Where(i => 0 < Agent.Inventory.Value.GetCapacity(i));
				if (itemsWithCapacity.None()) return false;

				var target = World.ItemDrops.AllActive
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

			bool IsAnyBuildingWithInventoryReachable()
			{
				var currentlyValidItems = validItems.Where(i => Agent.Inventory.Value.Any(i));

				if (currentlyValidItems.None()) return false; // There are zero of any valid items...
				
				var target = DwellerUtility.CalculateNearestEntrance(
					Agent.Position.Value,
					World.Buildings.AllActive,
					b =>
					{
						if (!b.InventoryPermission.Value.CanDeposit(Agent.Job.Value)) return false;
						return currentlyValidItems.Any(i => 0 < b.Inventory.Value.GetCapacity(i));
					},
					out targetPath,
					out _
				);

				return target != null;
			}

			public override void Transition()
			{
				cleanupState.ResetCleanupCount();
			}
		}
	}
}