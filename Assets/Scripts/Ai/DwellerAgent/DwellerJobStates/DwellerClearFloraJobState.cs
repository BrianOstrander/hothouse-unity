using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai
{
	public class DwellerClearFloraJobState : DwellerJobState<DwellerClearFloraJobState>
	{
		public override Jobs Job => Jobs.ClearFlora;

		public override void OnInitialize()
		{
			var validJobs = new[] { Jobs.ClearFlora };
			var validItems = new[] { Item.Types.Stalks, Item.Types.Rations };
			
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
					ToItemCleanupOnValidInventory.InventoryTrigger.OnFull,
					validJobs,
					validItems
				),
				
				new ToAttackNearestFlora(attackState),
				
				new ToIdleOnShiftEnd(this),
				
				new ToNavigateToNearestFlora(),
				
				new ToItemCleanupOnValidInventory(
					cleanupState,
					ToItemCleanupOnValidInventory.InventoryTrigger.OnGreaterThanZero,
					validJobs,
					validItems
				),
				new ToItemCleanupOnValidInventory(
					cleanupState,
					ToItemCleanupOnValidInventory.InventoryTrigger.OnEmpty,
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
							var hasOverflow = Agent.InventoryCapacity.Value.AddClamped(
								Agent.Inventory.Value,
								itemDrops,
								out var clamped,
								out var overflow
							);

							Agent.Inventory.Value = clamped;

							if (!hasOverflow) return;

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
					.FirstOrDefault();

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