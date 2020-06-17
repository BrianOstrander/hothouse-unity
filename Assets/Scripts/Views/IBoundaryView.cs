using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.NumberDemon;
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
		
		public static bool BoundaryContains(
			this IBoundaryView view,
			Vector3 position
		)
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

			foreach (var collider in view.BoundaryColliders)
			{
				if (collider.Collider.Raycast(upRay, out _, MaximumHitDistance))
				{
					if (collider.Collider.Raycast(downRay, out _, MaximumHitDistance)) return true;
				}
			}
			
			return false;
		}

		public static Vector3? BoundaryRandomPoint(
			this IBoundaryView view,
			Demon generator
		)
		{
			if (view.BoundaryColliders == null || view.BoundaryColliders.None()) return null;
			
			Vector3? result = null;
			
			var collider = generator.GetNextFrom(view.BoundaryColliders).Collider;

			var center = collider.transform.position;
			
			switch (collider)
			{
				case BoxCollider box:
					center = new Vector3(
						generator.GetNextFloat(box.bounds.min.x, box.bounds.max.x),
						Mathf.Lerp(box.bounds.min.y, box.bounds.max.y, 0.5f),
						generator.GetNextFloat(box.bounds.min.z, box.bounds.max.z)
					);
					
					break;
				default:
					Debug.LogError("Unrecognized type: "+collider.GetType());
					break;
			}
			
			var angle = generator.GetNextFloat(-1f, 1f);
			
			var direction = new Vector3(
				Mathf.Cos(angle),
				0f,
				Mathf.Sin(angle)
			);
			
			var ray0 = new Ray(
				center - (direction * MaximumHitDistance),
				direction
			);
			
			var ray1 = new Ray(
				center + (direction * MaximumHitDistance),
				-direction
			);

			if (collider.Raycast(ray0, out var hit0, MaximumHitDistance))
			{
				if (collider.Raycast(ray1, out var hit1, MaximumHitDistance))
				{
					return Vector3.Lerp(hit0.point, hit1.point, generator.NextFloat);
				}
			}

			return result;
		}
	}
}