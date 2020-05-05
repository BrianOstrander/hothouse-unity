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
		static ItemCacheBuildingModel GetNearestItemCache(
			GameModel world,
			DwellerModel agent,
			out NavMeshPath path,
			out Vector3 entrancePosition
		)
		{
			var pathResult = new NavMeshPath();
			var entranceResult = Vector3.zero;

			var result = world.ItemCaches.Value
				.Where(t => 0 < t.Inventory.Value.GetCapacity(Item.Types.Stalks))
				.OrderBy(t => Vector3.Distance(agent.Position.Value, t.Position.Value))
				.FirstOrDefault(
					t =>
					{
						foreach (var entrance in t.Entrances.Value)
						{
							if (entrance.State != BuildingModel.Entrance.States.Available) continue;

							var hasPath = NavMesh.CalculatePath(
								agent.Position.Value,
								entrance.Position,
								NavMesh.AllAreas,
								pathResult
							);

							if (hasPath)
							{
								entranceResult = entrance.Position;
								return true;
							}
						}

						return false;
					}
				);

			path = pathResult;
			entrancePosition = entranceResult;
			
			return result;
		}

		public override DwellerModel.Jobs Job => DwellerModel.Jobs.ClearFlora;

		public override void OnInitialize()
		{
			base.OnInitialize();

			var attackState = new DwellerAttackState<DwellerClearFloraJobState>();
			var unloadItemsState = new DwellerUnloadItemsState<DwellerClearFloraJobState>();
			
			AddChildStates(
				attackState,
				unloadItemsState,
				new DwellerNavigateState<DwellerClearFloraJobState>()
			);
			
			AddTransitions(
				new ToUnloadItemsToNearestItemCache(unloadItemsState),
				new ToNavigateToNearestItemCache(),
				new ToAttackNearestFlora(attackState),
				new ToNavigateToNearestFlora()
			);
		}

		class ToUnloadItemsToNearestItemCache : AgentTransition<DwellerUnloadItemsState<DwellerClearFloraJobState>, GameModel, DwellerModel>
		{
			DwellerUnloadItemsState<DwellerClearFloraJobState> unloadState;
			ItemCacheBuildingModel target;

			public ToUnloadItemsToNearestItemCache(DwellerUnloadItemsState<DwellerClearFloraJobState> unloadState) => this.unloadState = unloadState;
			
			public override bool IsTriggered()
			{
				if (Agent.Inventory.Value[Item.Types.Stalks] == 0) return false;
				if (!Agent.Inventory.Value.IsFull(Item.Types.Stalks) && World.Flora.GetActive().Any(f => f.MarkedForClearing.Value)) return false;

				target = GetNearestItemCache(World, Agent, out _, out var entrancePosition);

				if (target == null) return false;

				return Mathf.Approximately(0f, Vector3.Distance(Agent.Position.Value.NewY(0f), entrancePosition.NewY(0f)));
			}

			public override void Transition()
			{
				unloadState.SetTarget(
					new DwellerUnloadItemsState<DwellerClearFloraJobState>.Target(
						target,
						Inventory.Populate(
							new Dictionary<Item.Types, int>()
							{
								{ Item.Types.Stalks, Agent.Inventory.Value[Item.Types.Stalks] }
							}
						)
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

				target = GetNearestItemCache(World, Agent, out targetPath, out _);

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
			
							Debug.LogWarning("TODO: figure out what to do with overflow");
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
	}
}