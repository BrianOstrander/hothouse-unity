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
		static ItemCacheBuildingModel GetNearestItemCacheWithStalkCapacity(
			GameModel world,
			DwellerModel agent,
			out NavMeshPath path,
			out Vector3 entrancePosition
		)
		{
			return DwellerUtility.CalculateNearestEntrance(
				agent.Position.Value,
				world.ItemCaches.Value,
				b => 0 < b.Inventory.Value.GetCapacity(Item.Types.Stalks),
				out path,
				out entrancePosition
			);
		}

		public override DwellerModel.Jobs Job => DwellerModel.Jobs.ClearFlora;

		public override void OnInitialize()
		{
			base.OnInitialize();

			var attackState = new DwellerAttackState<DwellerClearFloraJobState>();
			var transferItemsState = new DwellerTransferItemsState<DwellerClearFloraJobState>();
			
			AddChildStates(
				attackState,
				transferItemsState,
				new DwellerNavigateState<DwellerClearFloraJobState>()
			);
			
			AddTransitions(
				new ToUnloadItemsToNearestItemCache(transferItemsState),
				new ToNavigateToNearestItemCache(),
				new ToAttackNearestFlora(attackState),
				new ToNavigateToNearestFlora(),
				new ToLoadItemsFromNearestItemDrop(transferItemsState),
				new ToNavigateToNearestItemDrop()
			);
		}

		class ToUnloadItemsToNearestItemCache : AgentTransition<DwellerTransferItemsState<DwellerClearFloraJobState>, GameModel, DwellerModel>
		{
			DwellerTransferItemsState<DwellerClearFloraJobState> transferState;
			ItemCacheBuildingModel target;

			public ToUnloadItemsToNearestItemCache(DwellerTransferItemsState<DwellerClearFloraJobState> transferState) => this.transferState = transferState;
			
			public override bool IsTriggered()
			{
				if (Agent.Inventory.Value[Item.Types.Stalks] == 0) return false;
				if (!Agent.Inventory.Value.IsFull(Item.Types.Stalks) && World.Flora.GetActive().Any(f => f.MarkedForClearing.Value)) return false;

				target = GetNearestItemCacheWithStalkCapacity(World, Agent, out _, out var entrancePosition);

				if (target == null) return false;

				return Mathf.Approximately(0f, Vector3.Distance(Agent.Position.Value.NewY(0f), entrancePosition.NewY(0f)));
			}

			public override void Transition()
			{
				transferState.SetTarget(
					new DwellerTransferItemsState<DwellerClearFloraJobState>.Target(
						i => target.Inventory.Value = i,
						() => target.Inventory.Value,
						i => Agent.Inventory.Value = i,
						() => Agent.Inventory.Value,
						Inventory.Populate(
							new Dictionary<Item.Types, int>()
							{
								{ Item.Types.Stalks, Agent.Inventory.Value[Item.Types.Stalks] }
							}
						),
						Agent.UnloadCooldown.Value
					)
				);
			}
		}
		
		class ToNavigateToNearestItemCache : AgentTransition<DwellerNavigateState<DwellerClearFloraJobState>, GameModel, DwellerModel>
		{
			ItemCacheBuildingModel target;
			NavMeshPath targetPath = new NavMeshPath();
			
			public override bool IsTriggered()
			{
				if (Agent.Inventory.Value[Item.Types.Stalks] == 0) return false;
				if (!Agent.Inventory.Value.IsFull(Item.Types.Stalks) && World.Flora.GetActive().Any(f => f.MarkedForClearing.Value)) return false;

				target = GetNearestItemCacheWithStalkCapacity(World, Agent, out targetPath, out _);

				return target != null;
			}

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(targetPath);
			}
		}

		class ToAttackNearestFlora : AgentTransition<DwellerAttackState<DwellerClearFloraJobState>, GameModel, DwellerModel>
		{
			DwellerAttackState<DwellerClearFloraJobState> attackState;
			FloraModel targetFlora;

			public ToAttackNearestFlora(DwellerAttackState<DwellerClearFloraJobState> attackState) => this.attackState = attackState;
			
			public override bool IsTriggered()
			{
				targetFlora = World.Flora.GetActive().FirstOrDefault(
					flora =>
					{
						if (flora.PooledState.Value == PooledStates.InActive) return false;
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
									itemDrop.Job.Value = DwellerModel.Jobs.ClearFlora;
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
				target = World.Flora.GetActive()
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
		
		class ToLoadItemsFromNearestItemDrop : AgentTransition<DwellerTransferItemsState<DwellerClearFloraJobState>, GameModel, DwellerModel>
		{
			DwellerTransferItemsState<DwellerClearFloraJobState> transferState;
			ItemDropModel target;

			public ToLoadItemsFromNearestItemDrop(DwellerTransferItemsState<DwellerClearFloraJobState> transferState) => this.transferState = transferState;
			
			public override bool IsTriggered()
			{
				if (Agent.Inventory.Value.IsFull(Item.Types.Stalks)) return false;

				target = World.ItemDrops.GetActive()
					.Where(t => t.Job.Value == DwellerModel.Jobs.ClearFlora && t.Inventory.Value.Any(Item.Types.Stalks))
					.OrderBy(t => Vector3.Distance(Agent.Position.Value, t.Position.Value))
					.FirstOrDefault();
				
				if (target == null) return false;

				return Mathf.Approximately(0f, Vector3.Distance(Agent.Position.Value.NewY(0f), target.Position.Value.NewY(0f)));
			}

			public override void Transition()
			{
				transferState.SetTarget(
					new DwellerTransferItemsState<DwellerClearFloraJobState>.Target(
						i => Agent.Inventory.Value = i,
						() => Agent.Inventory.Value,
						i => target.Inventory.Value = i,
						() => target.Inventory.Value,
						Inventory.Populate(
							new Dictionary<Item.Types, int>()
							{
								{ Item.Types.Stalks, target.Inventory.Value[Item.Types.Stalks] }
							}
						),
						Agent.UnloadCooldown.Value
					)
				);
			}
		}
		
		class ToNavigateToNearestItemDrop : AgentTransition<DwellerNavigateState<DwellerClearFloraJobState>, GameModel, DwellerModel>
		{
			ItemDropModel target;
			NavMeshPath targetPath = new NavMeshPath();
			
			public override bool IsTriggered()
			{
				if (Agent.Inventory.Value.IsFull(Item.Types.Stalks)) return false;

				target = World.ItemDrops.GetActive()
					.Where(t => t.Job.Value == DwellerModel.Jobs.ClearFlora && t.Inventory.Value.Any(Item.Types.Stalks))
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