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
		public static M CalculateNearestEntrance<M>(
			Vector3 beginPosition,
			IEnumerable<M> buildings,
			Func<M, bool> buildingPredicate,
			out NavMeshPath path,
			out Vector3 entrancePosition
		)
			where M : BuildingModel
		{
			var pathResult = new NavMeshPath();
			var entranceResult = Vector3.zero;

			// Debug.Log("nearest entrance buildings: "+buildings.Count());
			
			var result = buildings
				.Where(buildingPredicate)
				.OrderBy(t => Vector3.Distance(beginPosition, t.Position.Value))
				.FirstOrDefault(
					t =>
					{
						foreach (var entrance in t.Entrances.Value)
						{
							if (entrance.State != Entrance.States.Available) continue;

							var hasPath = NavMesh.CalculatePath(
								beginPosition,
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
	}
}