using System.Linq;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class ClearerJobState : JobState<ClearerJobState>
	{
		public override Jobs Job => Jobs.Clearer;

		public override void OnInitialize()
		{
			var validJobs = new[] { Jobs.Clearer, Jobs.None };
			var validCleanupItems = Inventory.ValidTypes;
			
			var attackState = new AttackState<ClearerJobState>();
			var cleanupState = new ItemCleanupState<ClearerJobState>(
				validJobs
			);
			var timeoutState = new TimeoutState<ClearerJobState>();
			
			AddChildStates(
				attackState,
				cleanupState,	
				timeoutState,
				new NavigateState<ClearerJobState>()
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
				new DropItemsTransition<ClearerJobState>(timeoutState),
				new NavigateToNearestLightTransition<ClearerJobState>()
			);
		}

		class ToAttackNearestClearable : AgentTransition<AttackState<ClearerJobState>, GameModel, DwellerModel>
		{
			AttackState<ClearerJobState> attackState;
			IClearableModel target;

			public ToAttackNearestClearable(AttackState<ClearerJobState> attackState) => this.attackState = attackState;
			
			public override bool IsTriggered()
			{
				target = Game.GetClearables().FirstOrDefault(
					clearable =>
					{
						if (!clearable.Clearable.IsMarkedForClearance.Value) return false;
						if (clearable.LightSensitive.IsNotLit) return false;
						return Vector3.Distance(Agent.Transform.Position.Value, clearable.Transform.Position.Value) <= (Agent.MeleeRange.Value + clearable.Clearable.MeleeRangeBonus.Value);
					}
				);

				return target != null;
			}

			public override void Transition()
			{
				var itemDrops = target.Clearable.ItemDrops.Value;
				attackState.SetTarget(
					new AttackState<ClearerJobState>.Target(
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

							Game.ItemDrops.Activate(
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
		
		class ToNavigateToNearestClearable : AgentTransition<NavigateState<ClearerJobState>, GameModel, DwellerModel>
		{
			IClearableModel target;
			
			public override bool IsTriggered()
			{
				target = Game.GetClearables()
					.Where(t => t.Clearable.IsMarkedForClearance.Value && t.LightSensitive.IsLit)
					.OrderBy(t => Vector3.Distance(Agent.Transform.Position.Value, t.Transform.Position.Value))
					.FirstOrDefault();

				return target != null;
			}

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Calculating(
					Agent.Transform.Position.Value,
					target.Transform.Position.Value,
					Agent.MeleeRange.Value + target.Clearable.MeleeRangeBonus.Value
				);
			}
		}
	}
}