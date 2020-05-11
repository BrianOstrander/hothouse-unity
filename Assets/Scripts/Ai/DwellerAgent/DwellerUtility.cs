using System;
using System.Linq;
using System.Collections.Generic;
using Lunra.Hothouse.Models;
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
	}
}