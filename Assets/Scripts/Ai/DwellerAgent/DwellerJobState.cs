using System.Linq;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;

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
			Inventory.Types[] validItemsToCleanup;

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

				Inventory.Types[] validItemsWithBuildingInventoryCapacity;
				
				switch (inventoryTrigger)
				{
					case InventoryTrigger.OnEmpty:
						// Since the inventory is empty, only check items we're allowed to pick up.
						if (!IsAnyBuildingWithInventoryReachable(validItems, out validItemsWithBuildingInventoryCapacity)) return false;
						return IsAnyValidItemDropReachable(
							validItemsWithBuildingInventoryCapacity,
							out validItemsToCleanup
						);
					case InventoryTrigger.OnFull:
					case InventoryTrigger.OnGreaterThanZero:
					case InventoryTrigger.OnGreaterThanZeroAndShiftOver:
						// Since we have items of any type in our inventory, check against all valid ones so we can
						// remove any random items wasting space in our inventory.
						if (!IsAnyBuildingWithInventoryReachable(Inventory.ValidTypes, out validItemsWithBuildingInventoryCapacity)) return false;
						return IsAnyBuildingMatchingCurrentInventoryReachable(out validItemsToCleanup);
				}
				
				Debug.LogError("Unrecognized "+nameof(inventoryTrigger)+": "+inventoryTrigger);
				return false;
			}

			/// <summary>
			/// Takes a list of items and finds the first item drop containing any of the specified items. If additional
			/// specified items happen to be available at that drop, they will be included as well.
			/// </summary>
			/// <remarks>
			/// This does not check if there is storage available for any of the specified items, so make sure to check
			/// after that or to only specify items that you know there is capacity for. 
			/// </remarks>
			/// <param name="items"></param>
			/// <param name="itemsAvailable"></param>
			/// <returns></returns>
			bool IsAnyValidItemDropReachable(
				Inventory.Types[] items,
				out Inventory.Types[] itemsAvailable
			)
			{
				itemsAvailable = null;
				foreach (var item in items)
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
						out var target
					);
					if (isValid)
					{
						itemsAvailable = target.Inventory.Value.Entries
							.Where(i => 0 < i.Weight && items.Contains(i.Type))
							.Select(i => i.Type)
							.ToArray();
						return true;
					}
				}

				return false;
			}

			/// <summary>
			/// Takes a list of items and finds the first building that has capacity for storing any of them. If
			/// additional specified items happen to fit in that building, they will be included as well.
			/// </summary>
			/// <remarks>
			///	This does not check if these items are in the dweller's inventory or available as a drop, so make sure
			/// to check that the returned items are available somewhere.
			/// </remarks>
			/// <param name="items"></param>
			/// <param name="itemsAvailable"></param>
			/// <returns></returns>
			bool IsAnyBuildingWithInventoryReachable(
				IEnumerable<Inventory.Types> items,
				out Inventory.Types[] itemsAvailable
			)
			{
				Inventory.Types[] itemsWithBuildingInventoryCapacityResult = null;
				
				var target = DwellerUtility.CalculateNearestLitOperatingEntrance(
					Agent.Position.Value,
					out _,
					out _,
					b =>
					{
						if (!b.InventoryPermission.Value.CanDeposit(Agent.Job.Value)) return false;
						
						var itemsWithCapacity = items.Where(i => 0 < b.InventoryCapacity.Value.GetCapacityFor(b.Inventory.Value, i)).ToArray();
						
						if (itemsWithCapacity.Any())
						{
							itemsWithBuildingInventoryCapacityResult = itemsWithCapacity.ToArray();
							return true;
						}

						return false;
					},
					World.Buildings.AllActive
				);

				itemsAvailable = itemsWithBuildingInventoryCapacityResult;
				return target != null;
			}
			
			/// <summary>
			/// Checks to see if any items in the current dweller's inventory has storage at an available building. This
			/// ignores the list of specified valid items since there may be junk in the dweller's inventory that needs
			/// to be disposed of. 
			/// </summary>
			/// <remarks>
			/// This will find the first building with any capacity for items in the dweller's inventory, if additional
			/// capacity is available those items will be returned as well.
			/// </remarks>
			/// <param name="itemsAvailable"></param>
			/// <returns></returns>
			bool IsAnyBuildingMatchingCurrentInventoryReachable(out Inventory.Types[] itemsAvailable)
			{
				return IsAnyBuildingWithInventoryReachable(
					Agent.Inventory.Value.Entries
						.Where(e => 0 < e.Weight)
						.Select(e => e.Type),
					out itemsAvailable
				);
			}

			public override void Transition()
			{
				cleanupState.ResetCleanupCount(validItemsToCleanup);
			}
		}
	}
}