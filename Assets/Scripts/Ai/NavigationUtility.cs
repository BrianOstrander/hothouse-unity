using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai
{
	public static class NavigationUtility
	{
		public static bool CalculateNearestFloor(
			Vector3 position,
			out NavMeshHit navHit,
			out RaycastHit physicsHit,
			out string roomId,
			float tolerance = 0.1f
		)
		{
			navHit = default;
			roomId = null;
			
			var raycastHit = Physics.Raycast(
				new Ray(
					position.NewY(8f),
					Vector3.down
				),
				out physicsHit,
				16f,
				LayerMasks.Floor
			);

			if (!raycastHit) return false;

			if (physicsHit.transform.GetAncestor<View>(v => v is IRoomIdView) is IRoomIdView roomIdView)
			{
				roomId = roomIdView.RoomId;
			}

			return NavMesh.SamplePosition(
				physicsHit.point,
				out navHit,
				tolerance,
				NavMesh.AllAreas
			);
		}
		
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

			try
			{
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
			}
			catch (InvalidOperationException)
			{
				return false;
			}

			return false;
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