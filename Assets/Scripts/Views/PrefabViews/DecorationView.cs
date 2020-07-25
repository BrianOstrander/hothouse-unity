using System;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class DecorationView : PrefabView, ICachableView
	{
		public static class Constants
		{
			public static class Names
			{
				public const string BoundaryCollider = "boundary_collider";
			}
			
			public static class Tags
			{
				public const string DecorationWall = "decoration_wall";
			}

			public static class Boundaries
			{
				public const float MaximumBoundary = 1000f;
			}
		}

		[SerializeField] float extentsLeft;
		[SerializeField] float extentsRight;
		[SerializeField] float extentForward;
		[SerializeField] float extentHeight;
		
		public float ExtentsLeft => extentsLeft;
		public float ExtentsRight => extentsRight;
		public float ExtentForward => extentForward;
		public float ExtentHeight => extentHeight;
		public float ExtentsLeftRightWidth => ExtentsLeft + ExtentsRight;


#if UNITY_EDITOR
		[ContextMenu("Calculate Cached Data")]
		public void MenuTriggerCalculateCachedData()
		{
			Undo.RecordObject(this, "Calculate Cached Data");
			CalculateCachedData();
			PrefabUtility.RecordPrefabInstancePropertyModifications(this);
		}
		
		public void CalculateCachedData()
		{
			var boundaryCollider = transform.GetFirstDescendantOrDefault<BoxCollider>(d => d.gameObject.name == Constants.Names.BoundaryCollider);

			if (boundaryCollider == null) throw new Exception("Could not find "+Constants.Names.BoundaryCollider);

			var anyFailures = boundaryCollider.Raycast(
				new Ray(Vector3.zero + (Vector3.left * Constants.Boundaries.MaximumBoundary), Vector3.right),
				out var leftSideHit,
				Constants.Boundaries.MaximumBoundary
			);
			
			anyFailures &= boundaryCollider.Raycast(
				new Ray(Vector3.zero + (Vector3.right * Constants.Boundaries.MaximumBoundary), Vector3.left),
				out var rightSideHit,
				Constants.Boundaries.MaximumBoundary
			);
			
			anyFailures &= boundaryCollider.Raycast(
				new Ray(Vector3.zero + (Vector3.forward * Constants.Boundaries.MaximumBoundary), Vector3.back),
				out var forwardSideHit,
				Constants.Boundaries.MaximumBoundary
			);
			
			anyFailures &= boundaryCollider.Raycast(
				new Ray(Vector3.zero + (Vector3.up * Constants.Boundaries.MaximumBoundary), Vector3.down),
				out var topSideHit,
				Constants.Boundaries.MaximumBoundary
			);

			if (!anyFailures) throw new Exception("Could not raycast boundaries");

			extentsLeft = Constants.Boundaries.MaximumBoundary - leftSideHit.distance;
			extentsRight = Constants.Boundaries.MaximumBoundary - rightSideHit.distance;
			extentForward = Constants.Boundaries.MaximumBoundary - forwardSideHit.distance;
			extentHeight = Constants.Boundaries.MaximumBoundary - topSideHit.distance;
		}

		void OnDrawGizmosSelected()
		{
			if (Application.isPlaying) return;
			
			Gizmos.color = Color.magenta;
			
			Gizmos.DrawLine(
				Vector3.left * ExtentsLeft,
				Vector3.left * (ExtentsLeft + 1f)
			);
			
			Gizmos.DrawLine(
				Vector3.right * ExtentsRight,
				Vector3.right * (ExtentsRight + 1f)
			);
			
			Gizmos.DrawLine(
				Vector3.forward * ExtentForward,
				Vector3.forward * (ExtentForward + 1f)
			);
			
			Gizmos.DrawLine(
				Vector3.up * extentHeight,
				Vector3.up * (extentHeight + 1f)
			);
		}

		void OnDrawGizmos()
		{
			if (Application.isPlaying) return;
			
			Gizmos.color = Color.blue.NewA(0.5f);
			Gizmos.DrawLine(Vector3.zero, Vector3.forward * 100f);
			
			Gizmos.color = Color.red.NewA(0.5f);
			Gizmos.DrawLine(Vector3.right * -50f, Vector3.right * 50f);
			
			Gizmos.color = Color.green.NewA(0.5f);
			Gizmos.DrawLine(Vector3.zero, Vector3.up * 100f);
		}
#endif
	}
}