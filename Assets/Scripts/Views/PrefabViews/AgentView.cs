using System;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class AgentView : PrefabView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] string deathEffectId;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public event Action<string> RoomChanged;
		#endregion
		
		#region Reverse Bindings
		public string DeathEffectId => deathEffectId;
		#endregion

		int roomBoundaryLayer;
		
		public override void Cleanup()
		{
			roomBoundaryLayer = LayerMask.NameToLayer(LayerNames.RoomBoundary);
			RoomChanged = ActionExtensions.GetEmpty<string>();
			
			base.Cleanup();
		}

		void OnTriggerEnter(Collider other)
		{
			if (other.gameObject.layer != roomBoundaryLayer) return;
			
			RoomChanged(other.transform.GetAncestor<RoomView>().RoomId);
		}
	}

}