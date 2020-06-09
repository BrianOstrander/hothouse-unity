using System;
using System.Linq;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class DoorView : PrefabView, IEnterableView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] GameObject door;
		[SerializeField] Transform[] entrances = new Transform[0];
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Bindings
		public bool IsOpen { set => door.SetActive(!value); }
		
		public event Action<bool> Highlight;
		public event Action Click;
		#endregion
		
		#region Reverse Bindings
		public Vector3[] Entrances => entrances.Select(e => e.position).ToArray();
		#endregion

		public override void Reset()
		{
			base.Reset();

			IsOpen = false;
			
			Highlight = null;
			Click = null;
		}
		
		#region Events
		public void OnPointerEnter() => Highlight?.Invoke(true);
		public void OnPointerExit() => Highlight?.Invoke(false);
		public void OnPointerClick() => Click?.Invoke();
		#endregion
	}
}