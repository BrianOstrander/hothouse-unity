using System.Linq;
using Lunra.Hothouse.Models;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public interface IBoundaryView : IView
	{
		ColliderCache[] BoundaryColliders { get; }
	}

	public static class IBoundaryViewExtensions
	{
		const float MaximumHitDistance = 1000f;
		
		public static bool BoundaryContains(this IBoundaryView view, Vector3 position)
		{
			if (!view.RootGameObject.activeInHierarchy) Debug.LogError("Attempting to check bounds on disabled view, unpredictable behaviour may occur");

			var upRay = new Ray(
				position + (Vector3.down * MaximumHitDistance),
				Vector3.up
			);
			
			var downRay = new Ray(
				position + (Vector3.up * MaximumHitDistance),
				Vector3.down
			);

			Debug.DrawRay(upRay.origin, upRay.direction, Color.yellow, 1f);
			
			foreach (var collider in view.BoundaryColliders)
			{
				if (collider.Collider.Raycast(upRay, out _, MaximumHitDistance))
				{
					if (collider.Collider.Raycast(downRay, out _, MaximumHitDistance)) return true;
				}
			}
			
			return false;
		}
	}
}