using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.WildVacuum.Debugging
{
	public class DebugAgent : MonoBehaviour
	{
		[SerializeField] string targetName;
		[SerializeField] NavMeshAgent navMeshAgent;

		Transform target;
		Material material;

		List<MeshRenderer> renderers = new List<MeshRenderer>();
		
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
			if (target == null)
			{
				target = GameObject.Find(targetName)?.transform;
				return;
			}

			
			navMeshAgent.SetDestination(target.position);

			switch (navMeshAgent.pathStatus)
			{
				case NavMeshPathStatus.PathComplete: material.color = Color.green; break;
				case NavMeshPathStatus.PathPartial: material.color = Color.yellow; break;
				case NavMeshPathStatus.PathInvalid: material.color = Color.red; break;
			}
		}

		void OnDrawGizmosSelected()
		{
			Gizmos.DrawLine(transform.position, transform.position + (transform.forward * 3f));
		}
	}
}