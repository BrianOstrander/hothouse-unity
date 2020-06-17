using System.Linq;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public class DwellerClearerJobState : DwellerJobState<DwellerClearerJobState>
	{
		public override Jobs Job => Jobs.Clearer;

		public override void OnInitialize()
		{
			var validJobs = new[] { Jobs.Clearer, Jobs.None };
			var validCleanupItems = Inventory.ValidTypes;
			
			var attackState = new DwellerAttackState<DwellerClearerJobState>();
			var cleanupState = new DwellerItemCleanupState<DwellerClearerJobState>(
				validJobs
			);
			var timeoutState = new DwellerTimeoutState<DwellerClearerJobState>();
			
			AddChildStates(
				attackState,
				cleanupState,	
				timeoutState,
				new DwellerNavigateState<DwellerClearerJobState>()
			);
			
			AddTransitions(
				new ToIdleOnJobUnassigned(this),
			
				new ToItemCleanupOnValidInventory(
					cleanupState,
					ToItemCleanupOnValidInventory.InventoryTrigger.OnFull,
					validJobs,
					validCleanupItems
				),
				
				new ToItemCleanupOnValidInventory(
					cleanupState,
					ToItemCleanupOnValidInventory.InventoryTrigger.OnGreaterThanZeroAndShiftOver,
					validJobs,
					validCleanupItems
				),
				
				new ToAttackNearestClearable(attackState),
				
				new ToIdleOnShiftEnd(this),
				
				new ToNavigateToNearestClearable(),
				
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
				new DwellerDropItemsTransition<DwellerClearerJobState>(timeoutState),
				new DwellerNavigateToNearestLightTransition<DwellerClearerJobState>()
			);
		}

		class ToAttackNearestClearable : AgentTransition<DwellerAttackState<DwellerClearerJobState>, GameModel, DwellerModel>
		{
			DwellerAttackState<DwellerClearerJobState> attackState;
			IClearableModel target;

			public ToAttackNearestClearable(DwellerAttackState<DwellerClearerJobState> attackState) => this.attackState = attackState;
			
			public override bool IsTriggered()
			{
				target = World.GetClearables().FirstOrDefault(
					clearable =>
					{
						if (!clearable.IsMarkedForClearance.Value) return false;
						if (clearable.LightSensitive.IsNotLit) return false;
						return Vector3.Distance(Agent.Transform.Position.Value, clearable.Transform.Position.Value) <= (Agent.MeleeRange.Value + clearable.MeleeRangeBonus.Value);
					}
				);

				return target != null;
			}

			public override void Transition()
			{
				var itemDrops = target.ItemDrops.Value;
				attackState.SetTarget(
					new DwellerAttackState<DwellerClearerJobState>.Target(
						() => target.Id.Value,
						() => target.Health.IsDestroyed,
						() =>
						{
							var attackResult = Damage.ApplyGeneric(
								Agent.MeleeDamage.Value,
								Agent,
								target
							);

							if (!attackResult.IsTargetDestroyed) return attackResult;
							
							var hasOverflow = Agent.InventoryCapacity.Value.AddClamped(
								Agent.Inventory.Value,
								itemDrops,
								out var clamped,
								out var overflow
							);

							Agent.Inventory.Value = clamped;

							if (!hasOverflow) return attackResult;

							World.ItemDrops.Activate(
								"default",
								target.RoomTransform.Id.Value,
								target.Transform.Position.Value,
								Quaternion.identity,
								itemDrop =>
								{
									itemDrop.Inventory.Value = overflow;
									itemDrop.Job.Value = Jobs.Clearer;
								}
							);

							return attackResult;
						}
					)
					/*
					new DwellerAttackState<DwellerClearerJobState>.Target(
						() => target.Id.Value,
						() => target.Health.Current.Value,
						health => target.Health.Current.Value = health,
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
								"default",
								target.RoomTransform.Id.Value,
								target.Transform.Position.Value,
								Quaternion.identity,
								itemDrop =>
								{
									itemDrop.Inventory.Value = overflow;
									itemDrop.Job.Value = Jobs.Clearer;
								}
							);
						}
					)
					*/
				);
			}
		}
		
		class ToNavigateToNearestClearable : AgentTransition<DwellerNavigateState<DwellerClearerJobState>, GameModel, DwellerModel>
		{
			IClearableModel target;
			
			public override bool IsTriggered()
			{
				target = World.GetClearables()
					.Where(t => t.IsMarkedForClearance.Value && t.LightSensitive.IsLit)
					.OrderBy(t => Vector3.Distance(Agent.Transform.Position.Value, t.Transform.Position.Value))
					.FirstOrDefault();

				return target != null;
			}

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Calculating(
					Agent.Transform.Position.Value,
					target.Transform.Position.Value,
					Agent.MeleeRange.Value + target.MeleeRangeBonus.Value
				);
			}
		}
	}
}