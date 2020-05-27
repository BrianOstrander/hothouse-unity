using System.Linq;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class DoorPrefabView : PrefabView, IEnterableView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] GameObject door;
		[SerializeField] Transform[] entrances = new Transform[0];
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Reverse Bindings
		public Vector3[] Entrances => entrances.Select(e => e.position).ToArray();
		#endregion

		public override void Reset()
		{
			base.Reset();
			
			door.SetActive(true);
		}

		[ContextMenu("Open Door")]
		public void Open()
		{
			door.SetActive(false);
		}
	}
}