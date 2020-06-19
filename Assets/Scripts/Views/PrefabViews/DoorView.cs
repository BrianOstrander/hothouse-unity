using System;
using System.Linq;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class DoorView : PrefabView, IEnterableView, IRoomIdView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] GameObject door;
		[SerializeField] Transform[] entrances = new Transform[0];
		[SerializeField] Transform[] doorAnchors = new Transform[0];
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Bindings
		public bool IsOpen { set => door.SetActive(!value); }
		public event Action<bool> Highlight;
		public event Action Click;
		#endregion
		
		#region Reverse Bindings
		public Vector3[] Entrances => entrances.Select(e => e.position).ToArray();
		public (Vector3 Position, Vector3 Forward)[] DoorAnchors => doorAnchors.Select(d => (d.position, d.forward)).ToArray();
		#endregion

		public override void Cleanup()
		{
			base.Cleanup();

			IsOpen = false;
			
			Highlight = null;
			Click = null;
		}
		
		#region Events
		public void OnPointerEnter() => Highlight?.Invoke(true);
		public void OnPointerExit() => Highlight?.Invoke(false);
		public void OnPointerClick() => Click?.Invoke();
		#endregion
		
		void OnDrawGizmosSelected()
		{
			if (doorAnchors == null) return;

			Gizmos.color = Color.blue;
			foreach (var doorAnchor in doorAnchors)
			{
				Gizmos.DrawRay(doorAnchor.position, doorAnchor.forward);
			}
		}
	}
}