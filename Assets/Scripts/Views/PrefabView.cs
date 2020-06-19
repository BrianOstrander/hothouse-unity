using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public interface IPrefabView : IView
	{
		string PrefabId { get; }
		string RoomId { get; set; }
		string ModelId { get; set; }
	}
	
	public class PrefabView : View, IPrefabView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		
		[SerializeField] string prefabId;
		public string PrefabId => prefabId;
		
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		public string RoomId { get; set; }
		public string ModelId { get; set; }

		public override void Reset()
		{
			base.Reset();

			RoomId = null;
			ModelId = null;
		}
	}

}