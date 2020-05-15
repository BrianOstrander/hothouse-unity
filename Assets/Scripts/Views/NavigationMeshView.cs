using System;
using System.Collections;
using Lunra.StyxMvp;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Views
{
	public class NavigationMeshView : View
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] NavMeshSurface navMeshSurface;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Bindings
		public void RebuildSurfaces(Action done = null)
		{
			if (navMeshSurface.navMeshData == null)
			{
				navMeshSurface.BuildNavMesh();
				done?.Invoke();
				return;
			}
			
			StartCoroutine(OnRebuildSurfaces(done));
		}

		IEnumerator OnRebuildSurfaces(Action done = null)
		{
			var operation = navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);

			while (!operation.isDone) yield return null;
			
			done?.Invoke();
		}
		#endregion

		// bool isNavMesSurfacehInitialized;
	}
 
}