using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using UnityEngine;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using UnityEngine.AI;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerClearFloraJobState : DwellerJobState<DwellerClearFloraJobState>
	{
		public override Jobs Job => Jobs.ClearFlora;

		public override void OnInitialize()
		{
			var validJobs = new[] { Jobs.ClearFlora };
			var validItems = new[] { Item.Types.Stalks };
			
			var attackState = new DwellerAttackState<DwellerClearFloraJobState>();
			var cleanupState = new DwellerItemCleanupState<DwellerClearFloraJobState>(
				validJobs,
				validItems
			);
			
			AddChildStates(
				attackState,
				cleanupState,	
				new DwellerNavigateState<DwellerClearFloraJobState>()
			);
			
			AddTransitions(
				new ToIdleOnJobUnassigned(this),
			
				new ToItemCleanupOnValidInventory(
					cleanupState,
					ToItemCleanupOnValidInventory.InventoryTrigger.NonZeroMaximumFull,
					validJobs,
					validItems
				),
				
				new ToAttackNearestFlora(attackState),
				
				new ToIdleOnShiftEnd(this),
				
				new ToNavigateToNearestFlora(),
				
				new ToItemCleanupOnValidInventory(
					cleanupState,
					ToItemCleanupOnValidInventory.InventoryTrigger.SomeOrNonZeroMaximumFull,
					validJobs,
					validItems
				),
				new ToItemCleanupOnValidInventory(
					cleanupState,
					ToItemCleanupOnValidInventory.InventoryTrigger.None,
					validJobs,
					validItems
				)
			);
		}

		class ToAttackNearestFlora : AgentTransition<DwellerAttackState<DwellerClearFloraJobState>, GameModel, DwellerModel>
		{
			DwellerAttackState<DwellerClearFloraJobState> attackState;
			FloraModel targetFlora;

			public ToAttackNearestFlora(DwellerAttackState<DwellerClearFloraJobState> attackState) => this.attackState = attackState;
			
			public override bool IsTriggered()
			{
				targetFlora = World.Flora.AllActive.FirstOrDefault(
					flora =>
					{
						if (!flora.MarkedForClearing.Value) return false;
						return Vector3.Distance(Agent.Position.Value, flora.Position.Value) < Agent.MeleeRange.Value;
					}
				);

				return targetFlora != null;
			}

			public override void Transition()
			{
				var itemDrops = targetFlora.ItemDrops.Value;
				attackState.SetTarget(
					new DwellerAttackState<DwellerClearFloraJobState>.Target(
						() => targetFlora.Id.Value,
						() => targetFlora.Health.Value,
						health => targetFlora.Health.Value = health,
						() =>
						{
							Agent.Inventory.Value = Agent.Inventory.Value.Add(
								itemDrops,
								out var overflow
							);

							if (overflow.IsEmpty) return;
							
							World.ItemDrops.Activate(
								itemDrop =>
								{
									itemDrop.RoomId.Value = targetFlora.RoomId.Value;
									itemDrop.Position.Value = targetFlora.Position.Value;
									itemDrop.Rotation.Value = Quaternion.identity;
									itemDrop.Inventory.Value = overflow;
									itemDrop.Job.Value = Jobs.ClearFlora;
								}
							);
						}
					)
				);
			}
		}
		
		class ToNavigateToNearestFlora : AgentTransition<DwellerNavigateState<DwellerClearFloraJobState>, GameModel, DwellerModel>
		{
			FloraModel target;
			
			public override bool IsTriggered()
			{
				target = World.Flora.AllActive
					.Where(t => t.MarkedForClearing.Value)
					.OrderBy(t => Vector3.Distance(Agent.Position.Value, t.Position.Value))
					.ElementAtOrDefault(Agent.JobPriority.Value);

				return target != null;
			}

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Calculating(
					Agent.Position.Value,
					target.Position.Value,
					Agent.MeleeRange.Value
				);
			}
		}
	}
}