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
				Always = 10,
				OnEmpty = 20,
				OnFull = 40,
				OnGreaterThanZero = 50,
				OnGreaterThanZeroAndShiftOver = 60
			}

			public override string Name => base.Name + "." + inventoryTrigger;

			DwellerItemCleanupState<S> cleanupState;
			InventoryTrigger inventoryTrigger;
			Jobs[] validJobs;
			Inventory.Types[] validItems;
			
			public ToItemCleanupOnValidInventory(
				DwellerItemCleanupState<S> cleanupState,
				InventoryTrigger inventoryTrigger,
				Jobs[] validJobs,
				Inventory.Types[] validItems
			)
			{
				this.cleanupState = cleanupState;
				this.inventoryTrigger = inventoryTrigger;
				this.validJobs = validJobs;
				this.validItems = validItems;
			}

			public override bool IsTriggered()
			{
				if (inventoryTrigger == InventoryTrigger.Always) return true;
				
				switch (inventoryTrigger)
				{
					case InventoryTrigger.OnEmpty:
						if (!Agent.Inventory.Value.IsEmpty) return false;
						break;
					case InventoryTrigger.OnFull:
						if (Agent.InventoryCapacity.Value.IsNotFull(Agent.Inventory.Value)) return false;
						break;
					case InventoryTrigger.OnGreaterThanZero:
						if (Agent.Inventory.Value.IsEmpty) return false;
						break;
					case InventoryTrigger.OnGreaterThanZeroAndShiftOver:
						if (Agent.Inventory.Value.IsEmpty) return false;
						if (Agent.JobShift.Value.Contains(World.SimulationTime.Value)) return false;
						break;
				}
				
				if (!IsAnyBuildingWithInventoryReachable(Inventory.ValidTypes)) return false;

				switch (inventoryTrigger)
				{
					case InventoryTrigger.OnEmpty:
						return IsAnyValidItemDropReachable();
					case InventoryTrigger.OnFull:
					case InventoryTrigger.OnGreaterThanZero:
					case InventoryTrigger.OnGreaterThanZeroAndShiftOver:
						return IsAnyBuildingMatchingCurrentInventoryReachable();
				}
				
				Debug.LogError("Unrecognized "+nameof(inventoryTrigger)+": "+inventoryTrigger);
				return false;
			}

			bool IsAnyValidItemDropReachable()
			{
				foreach (var item in validItems)
				{
					var itemEnumerable = new[] { item };
					var isValid = DwellerUtility.CalculateNearestCleanupWithdrawal(
						Agent,
						World,
						itemEnumerable,
						validJobs,
						out _,
						out _,
						out _,
						out _
					);
					if (isValid && IsAnyBuildingWithInventoryReachable(itemEnumerable)) return true;
				}

				return false;
			}

			bool IsAnyBuildingWithInventoryReachable(IEnumerable<Inventory.Types> types)
			{
				var target = DwellerUtility.CalculateNearestLitOperatingEntrance(
					Agent.Position.Value,
					out _,
					out _,
					b =>
					{
						if (!b.InventoryPermission.Value.CanDeposit(Agent.Job.Value)) return false;
						return types.Any(i => 0 < b.InventoryCapacity.Value.GetCapacityFor(b.Inventory.Value + (i, 1), i));
					},
					World.Buildings.AllActive
				);

				return target != null;
			}
			
			bool IsAnyBuildingMatchingCurrentInventoryReachable()
			{
				return IsAnyBuildingWithInventoryReachable(
					Agent.Inventory.Value.Entries
						.Where(e => 0 < e.Weight)
						.Select(e => e.Type)
				);
			}

			public override void Transition()
			{
				cleanupState.ResetCleanupCount();
			}
		}
	}
}