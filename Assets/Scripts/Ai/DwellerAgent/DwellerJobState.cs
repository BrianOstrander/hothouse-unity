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
				OnGreaterThanZero = 50
			}

			public override string Name => base.Name + "." + inventoryTrigger;

			DwellerItemCleanupState<S> cleanupState;
			InventoryTrigger inventoryTrigger;
			Jobs[] validJobs;
			Inventory.Types[] validItems;
			
			NavMeshPath targetPath = new NavMeshPath();

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
				if (!IsAnyBuildingWithInventoryReachable()) return false;
				
				switch (inventoryTrigger)
				{
					case InventoryTrigger.OnEmpty:
						if (!Agent.Inventory.Value.IsEmpty) return false;
						return IsAnyValidItemDropReachable();
					case InventoryTrigger.OnFull:
						if (Agent.InventoryCapacity.Value.IsNotFull(Agent.Inventory.Value)) return false;
						return true;
					case InventoryTrigger.OnGreaterThanZero:
						if (Agent.Inventory.Value.IsEmpty) return false;
						if (Agent.InventoryCapacity.Value.IsFull(Agent.Inventory.Value)) return true;
						return true;
				}
				
				Debug.LogError("Unrecognized "+nameof(inventoryTrigger)+": "+inventoryTrigger);
				return false;
			}

			bool IsAnyValidItemDropReachable()
			{
				return DwellerUtility.CalculateNearestCleanupWithdrawal(
					Agent,
					World,
					validItems,
					validJobs,
					out _,
					out _,
					out _,
					out _
				);
			}

			bool IsAnyBuildingWithInventoryReachable()
			{
				var target = DwellerUtility.CalculateNearestOperatingEntrance(
					Agent.Position.Value,
					out targetPath,
					out _,
					b =>
					{
						if (!b.InventoryPermission.Value.CanDeposit(Agent.Job.Value)) return false;
						return validItems.Any(i => b.InventoryCapacity.Value.IsNotFull(b.Inventory.Value + (i, 1)));
					},
					World.Buildings.AllActive
				);

				return target != null;
			}

			public override void Transition()
			{
				cleanupState.ResetCleanupCount();
			}
		}
		
		protected class ToDropItems : AgentTransition<DwellerTimeoutState<S>, GameModel, DwellerModel>
		{
			DwellerTimeoutState<S> timeoutState;

			public ToDropItems(
				DwellerTimeoutState<S> timeoutState
			)
			{
				this.timeoutState = timeoutState;
			}

			public override bool IsTriggered() => !Agent.Inventory.Value.IsEmpty;

			public override void Transition()
			{
				World.ItemDrops.Activate(
					itemDrop =>
					{
						itemDrop.RoomId.Value = Agent.RoomId.Value;
						itemDrop.Position.Value = Agent.Position.Value;
						itemDrop.Rotation.Value = Quaternion.identity;
						itemDrop.Inventory.Value = Agent.Inventory.Value;
						itemDrop.Job.Value = Agent.Job.Value;
					}
				);
				
				Agent.Inventory.Value = Inventory.Empty;
				
				timeoutState.ConfigureForInterval(Interval.WithMaximum(Agent.DepositCooldown.Value));
			}
		}
	}
}