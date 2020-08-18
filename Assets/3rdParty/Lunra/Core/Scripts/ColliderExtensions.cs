using UnityEngine;

namespace Lunra.Core
{
	public static class ColliderExtensions
	{
		public static bool ClosestPointIsInside(
			this Collider collider,
			Vector3 position
		)
		{
			if (collider is MeshCollider meshCollider && !meshCollider.convex) return false;
			
			return Mathf.Approximately(Vector3.Distance(position, collider.ClosestPoint(position)), 0f);
		}
	}
}