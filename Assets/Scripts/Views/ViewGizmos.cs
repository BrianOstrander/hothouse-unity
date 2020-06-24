using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public static class ViewGizmos
	{
		public static void DrawDoorGizmo(params DoorCache[] doorDefinitions)
		{
			if (doorDefinitions == null) return;

			Handles.color = Color.yellow.NewA(0.5f);

			foreach (var door in doorDefinitions)
			{
				if (door.Anchor == null) continue;

				var edge0 = door.Anchor.position + (door.Anchor.right * (door.Size * 0.5f));
				var edge1 = door.Anchor.position + (door.Anchor.right * (door.Size * -0.5f));
				var edgeForward = door.Anchor.position + door.Anchor.forward * 2f;
				
				var offset = door.Anchor.forward * 0.1f;

				edge0 += offset;
				edge1 += offset;
				edgeForward += offset;

				Gizmos.color = Color.yellow;
				
				Gizmos.DrawLine(edge0, edge1);
				Gizmos.DrawLine(edge0, edgeForward);
				Gizmos.DrawLine(edge1, edgeForward);
				
				Gizmos.DrawLine(edge0, edge0 + (Vector3.up * 4f));
				Gizmos.DrawLine(edge1, edge1 + (Vector3.up * 4f));
				
				Handles.DrawWireDisc(edge0, Vector3.up, 0.2f);
			}
		}
	}
}