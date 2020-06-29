using System;
using System.Linq;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai
{
	public static class NavigationUtility
	{
		public static bool CalculateNearest(
			Vector3 beginPosition,
			out Navigation.Result result,
			params Navigation.Query[] queries
		)
		{
			result = default;
			Navigation.Result resultCached = default;
			
			var path = new NavMeshPath();

			var sortedQueries = queries
				.OrderBy(q => q.GetMinimumTargetDistance(beginPosition));
			
			foreach (var query in sortedQueries)
			{
				Navigation.Result validate(Navigation.Validation validation)
				{
					if (query.Validate == null)
					{
						return DefaultCalculatePathValidation(validation.Path) ? validation.GetValid() : validation.GetInValid();
					}

					return query.Validate(validation);
				}

				var sampleInRadius = !Mathf.Approximately(0f, query.MaximumRadius);

				foreach (var position in query.GetTargets(beginPosition))
				{
					var currentPosition = position;

					if (sampleInRadius)
					{
						var isOnNavigationMesh = NavMesh.SamplePosition(
							currentPosition,
							out var navMeshHit,
							query.MaximumRadius,
							NavMesh.AllAreas // TODO: This should probably be fed in
						);
						
						if (!isOnNavigationMesh) continue;

						currentPosition = navMeshHit.position;
					}
					
					var hasPath = CalculatePath(
						beginPosition,
						currentPosition,
						path,
						pathForValidation =>
						{
							resultCached = validate(query.GetValidation(currentPosition, pathForValidation));
							return resultCached.IsValid;
						}
					);
					
					if (hasPath)
					{
						result = resultCached;
						return true;
					}
				}
			}

			return false;
		}
		
		public static M CalculateNearestAvailableOperatingEntrance<M>(
			Vector3 beginPosition,
			out NavMeshPath path,
			out Vector3 entrancePosition,
			Func<M, bool> buildingPredicate,
			params M[] buildings
		)
			where M : BuildingModel
		{
			return CalculateNearestAvailableEntrance(
				beginPosition,
				out path,
				out entrancePosition,
				b => b.BuildingState.Value == BuildingStates.Operating && buildingPredicate(b),
				buildings
			);
		}

		public static M CalculateNearestAvailableEntrance<M>(
			Vector3 beginPosition,
			out NavMeshPath path,
			out Vector3 entrancePosition,
			params M[] models
		)
			where M : IEnterableModel
		{
			return CalculateNearestAvailableEntrance(
				beginPosition,
				out path,
				out entrancePosition,
				m => true,
				models
			);
		}

		public static M CalculateNearestAvailableEntrance<M>(
			Vector3 beginPosition,
			out NavMeshPath path,
			out Vector3 entrancePosition,
			Func<M, bool> predicate,
			params M[] models
		)
			where M : IEnterableModel
		{
			var modelsSorted = models
				.Where(b => b.Enterable.Entrances.Value.Any(e => e.State == Entrance.States.Available) && predicate(b))
				.OrderBy(t => Vector3.Distance(beginPosition, t.Transform.Position.Value));

			foreach (var model in modelsSorted)
			{
				var found = CalculateNearestEntrance(
					beginPosition,
					out path,
					out entrancePosition,
					model
				);

				if (found) return model;
			}

			path = new NavMeshPath();
			entrancePosition = Vector3.zero;
			
			return default;
		}

		public static bool CalculateNearestEntrance<M>(
			Vector3 beginPosition,
			out NavMeshPath path,
			out Vector3 entrancePosition,
			M model
		)
			where M : IEnterableModel
		{
			path = new NavMeshPath();
			entrancePosition = Vector3.zero;

			foreach (var entrance in model.Enterable.Entrances.Value.OrderBy(e => Vector3.Distance(e.Position, beginPosition)))
			{
				if (entrance.State != Entrance.States.Available) continue;

				var hasPath = CalculatePath(
					beginPosition,
					entrance.Position,
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
			Inventory.Types[] validItems,
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
			
			var itemsWithCapacity = validItems.Where(i => agent.Inventory.Capacity.Value.HasCapacityFor(agent.Inventory.All.Value, i));
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
						if (possibleItemDrop.Inventory.Available.Value.IsEmpty) return false;
						return itemsWithCapacity.Any(i => 0 < possibleItemDrop.Inventory.Available.Value[i]);
					}
				)
				.OrderBy(t => Vector3.Distance(agent.Transform.Position.Value, t.Transform.Position.Value))
				.FirstOrDefault(
					t => CalculatePath(
						agent.Transform.Position.Value,
						t.Transform.Position.Value,
						pathResult
					)
				);

			if (target == null) return false;

			path = pathResult;
			
			agent.Inventory.Capacity.Value.GetCapacityFor(agent.Inventory.All.Value)
				.Intersects(
					target.Inventory.Available.Value,
					out inventoryToWithdrawal
				);

			promise = new InventoryPromise(
				InstanceId.New(target),
				InstanceId.Null(), // THIS MAY CAUSE AN ERROR
				InventoryPromise.Operations.CleanupWithdrawal,
				inventoryToWithdrawal
			);
			
			return true;	
		}

		static bool CalculatePath(
			Vector3 beginPosition,
			Vector3 endPosition,
			NavMeshPath path,
			Func<NavMeshPath, bool> validation = null
		)
		{
			var foundPath = NavMesh.CalculatePath(
				beginPosition,
				endPosition,
				NavMesh.AllAreas, // TODO: This should probably be fed in
				path
			);

			if (!foundPath) return false;

			if (validation == null) return DefaultCalculatePathValidation(path);
			return validation(path);
		}

		static bool DefaultCalculatePathValidation(NavMeshPath path) => path.status == NavMeshPathStatus.PathComplete;
	}
}