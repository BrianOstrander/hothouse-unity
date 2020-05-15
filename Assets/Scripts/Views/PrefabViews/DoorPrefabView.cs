using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Views
{
	public class DoorPrefabView : PrefabView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] GameObject door;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
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