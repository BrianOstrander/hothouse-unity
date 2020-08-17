using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public interface IEnterableView : IView
	{
		GameObject EntrancesRoot { get; set; }
		Transform[] Entrances { get; set; }
	}

#if UNITY_EDITOR
	public static class IEnterableViewExtensions
	{
		public static void CalculateCachedEntrances(this IEnterableView enterableView)
		{
			var boundaries = new List<(Vector3 Position, Vector3 Normal, bool Hit)>();

			var physicsScene = enterableView.RootGameObject.scene.GetPhysicsScene();

			const float SampleDelta = 360f / 8f;
			const float SampleRadius = 100f;
			const float EntranceDistance = 1.05f;

			var origin = Vector3.up * 0.1f;
			
			for (var i = 0f; i < 360f; i += SampleDelta)
			{
				var direction = Quaternion.AngleAxis(i, Vector3.up) * Vector3.forward;
				var position = origin + (direction * EntranceDistance);
				
				var didHit = physicsScene.Raycast(
					origin + (direction * SampleRadius),
					-direction,
					out var hit,
					SampleRadius,
					LayerMasks.Default
				);
				
				if (didHit) position = hit.point + (direction * EntranceDistance);

				if (boundaries.Any(b => Vector3.Distance(b.Position, position) < EntranceDistance)) continue;
				
				boundaries.Add((position, direction, didHit));
			}

			if (boundaries.None(b => b.Hit))
			{
				boundaries.Clear();
				boundaries.Add((origin, Vector3.forward, false));
			}
			
			if (enterableView.EntrancesRoot != null) Object.DestroyImmediate(enterableView.EntrancesRoot);
			
			enterableView.EntrancesRoot = new GameObject("entrances");
			enterableView.EntrancesRoot.transform.SetParent(enterableView.RootTransform);

			var entrancesList = new List<Transform>();
			
			var index = 0;
			foreach (var boundary in boundaries)
			{
				var entrance = new GameObject("entrance_"+index);
				entrance.transform.SetParent(enterableView.EntrancesRoot.transform);
				entrance.transform.position = boundary.Position;
				entrance.transform.forward = boundary.Normal;
				
				entrancesList.Add(entrance.transform);
				
				index++;
			}

			enterableView.Entrances = entrancesList.ToArray();
		}
		
	}
#endif
}