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
	public static class DwellerUtility
	{
		public static M CalculateNearestOperatingEntrance<M>(
			Vector3 beginPosition,
			out NavMeshPath path,
			out Vector3 entrancePosition,
			Func<M, bool> buildingPredicate,
			params M[] buildings
		)
			where M : BuildingModel
		{
			return CalculateNearestEntrance(
				beginPosition,
				out path,
				out entrancePosition,
				b => b.BuildingState.Value == BuildingStates.Operating && buildingPredicate(b),
				buildings
			);
		}

		public static M CalculateNearestEntrance<M>(
			Vector3 beginPosition,
			out NavMeshPath path,
			out Vector3 entrancePosition,
			Func<M, bool> buildingPredicate,
			params M[] buildings
		)
			where M : BuildingModel
		{
			var pathResult = new NavMeshPath();
			var entranceResult = Vector3.zero;
	
			var result = buildings
				.Where(buildingPredicate)
				.OrderBy(t => Vector3.Distance(beginPosition, t.Position.Value))
				.FirstOrDefault(
					t => CalculateNearestEntrance(
						beginPosition,
						out pathResult,
						out entranceResult,
						t
					)
				);

			path = pathResult;
			entrancePosition = entranceResult;
			
			return result;
		}

		public static bool CalculateNearestEntrance<M>(
			Vector3 beginPosition,
			out NavMeshPath path,
			out Vector3 entrancePosition,
			M building
		)
			where M : BuildingModel
		{
			path = new NavMeshPath();
			entrancePosition = Vector3.zero;

			foreach (var entrance in building.Entrances.Value)
			{
				if (entrance.State != Entrance.States.Available) continue;

				var hasPath = NavMesh.CalculatePath(
					beginPosition,
					entrance.Position,
					NavMesh.AllAreas,
					path
				);

				if (hasPath)
				{
					entrancePosition = entrance.Position;
					return true;
				}
			}

			return false;
		}

		public static bool CalculateNearestCleanupWithdrawal(
			DwellerModel agent,
			GameModel world,
			Item.Types[] validItems,
			Jobs[] validJobs,
			out NavMeshPath path,
			out InventoryPromise promise,
			out Inventory inventoryToWithdrawal,
			out ItemDropModel target
		)
		{
			path = default;
			promise = default;
			inventoryToWithdrawal = default;
			target = default;
			
			var itemsWithCapacity = validItems.Where(i => agent.InventoryCapacity.Value.HasCapacityFor(agent.Inventory.Value, i));
			if (itemsWithCapacity.None())
			{
				return false;
			}
			
			var pathResult = new NavMeshPath();

			target = world.ItemDrops.AllActive
				.Where(
					possibleItemDrop =>
					{
						if (!validJobs.Contains(possibleItemDrop.Job.Value)) return false;
						var nonPromisedInventory = possibleItemDrop.Inventory.Value - possibleItemDrop.WithdrawalInventoryPromised.Value;
						if (nonPromisedInventory.IsEmpty) return false;
						return itemsWithCapacity.Any(i => 0 < nonPromisedInventory[i]);
					}
				)
				.OrderBy(t => Vector3.Distance(agent.Position.Value, t.Position.Value))
				.FirstOrDefault(
					t =>  NavMesh.CalculatePath(
						agent.Position.Value,
						t.Position.Value,
						NavMesh.AllAreas,
						pathResult
					)
				);

			if (target == null) return false;

			path = pathResult;
			
			agent.InventoryCapacity.Value.GetCapacityFor(agent.Inventory.Value)
				.Intersects(
					target.Inventory.Value - target.WithdrawalInventoryPromised.Value,
					out inventoryToWithdrawal
				);

			promise = new InventoryPromise(
				target.Id.Value,
				InventoryPromise.Operations.CleanupWithdrawal,
				inventoryToWithdrawal
			);
			
			return true;	
		}
	}
}