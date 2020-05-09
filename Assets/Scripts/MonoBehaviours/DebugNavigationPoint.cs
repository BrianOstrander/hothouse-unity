using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lunra.Hothouse.Services;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Debugging
{
	public class DebugNavigationPoint : MonoBehaviour
	{
		public class NavigationPath
		{
			public enum States
			{
				Unknown = 0,
				Navigating = 10,
				Complete = 20
			}
			
			public readonly Vector3[] Corners;
			public Vector3 Position { get; private set; }
			public States State { get; private set; }
			
			int nextCorner = 1;

			public NavigationPath(Vector3[] corners)
			{
				Corners = corners;
				Position = Corners.FirstOrDefault();
				State = States.Navigating;
			}
			
			public NavigationPath Update(float velocity)
			{
				while (nextCorner < Corners.Length && 0f < velocity)
				{		
					var distanceToNextCorner = Vector3.Distance(Position, Corners[nextCorner]);
					
					if (distanceToNextCorner <= velocity)
					{
						Position = Corners[nextCorner];
						nextCorner++;
						velocity -= distanceToNextCorner;
						continue;
					}

					Position += (Corners[nextCorner] - Position).normalized * velocity;
					break;
				}

				if (nextCorner == Corners.Length) State = States.Complete;

				return this;
			}
		}
		
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] string targetName;
		[SerializeField] float navigationVelocity;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		Transform target;
		Material material;

		List<MeshRenderer> renderers = new List<MeshRenderer>();

		bool hasInitialized;
		
		NavigationPath path;
		float pathLength;

		DateTime lastNavigationCalculation;

		void Awake()
		{
			foreach (var renderer in gameObject.GetComponentsInChildren<MeshRenderer>())
			{
				Debug.Log(renderer.gameObject.name);
				if (material == null)
				{
					material = new Material(renderer.material);
				}
		
				renderer.sharedMaterial = material;
				
				renderers.Add(renderer);
			}
		}

		void Update()
		{
			if (!App.HasInstance) return;

			if (!hasInitialized)
			{
				if (App.S.Is(typeof(GameState), StateMachine.Events.Idle))
				{
					lastNavigationCalculation = DateTime.Now;

					var game = (App.S.CurrentHandler as GameState).Payload.Game;

					game.LastNavigationCalculation.Changed += OnGameLastNavigationCalculation;

					hasInitialized = true;
				}
				else return;
			}
			
			if (target == null)
			{
				target = GameObject.Find(targetName)?.transform;
				return;
			}

			if (path == null)
			{
				Debug.Log("started calcin'");
				var navMeshPath = new NavMeshPath();
				NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, navMeshPath);

				switch (navMeshPath.status)
				{
					case NavMeshPathStatus.PathComplete: material.color = Color.green; break;
					case NavMeshPathStatus.PathInvalid: material.color = Color.red; break;
					case NavMeshPathStatus.PathPartial: material.color = Color.yellow; break;
				}
				
				path = new NavigationPath(navMeshPath.corners);
			}
			else if (path.Update(navigationVelocity * Time.deltaTime).State == NavigationPath.States.Complete)
			{
				path = null;
			}
		}
		
		#region Events
		void OnGameLastNavigationCalculation(DateTime dateTime)
		{
			if (dateTime < lastNavigationCalculation) return;
			lastNavigationCalculation = dateTime;
			
			RecalculatePath();
		}
		#endregion

		[ContextMenu("Recalculate Path")]
		void RecalculatePath()
		{
			path = null;
		}

		void OnDrawGizmos()
		{
			// Gizmos.DrawLine(transform.position, transform.position + (transform.forward * 3f));
			if (path == null) return;

			Gizmos.color = material.color;

			for (var i = 0; i < path.Corners.Length - 1; i++)
			{
				Gizmos.DrawLine(path.Corners[i], path.Corners[i + 1]);
			}
			
			Gizmos.DrawWireSphere(path.Position, 0.25f);
		}
	}
}