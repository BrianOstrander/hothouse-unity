using System;
using System.Linq;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public interface IPrefabView : IView
	{
		string PrefabId { get; set; }
		string[] PrefabTags { get; set; }
		string RoomId { get; set; }
		string ModelId { get; set; }
	}
	
	public class PrefabView : View, IPrefabView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		
		[SerializeField] string prefabId;
		public string PrefabId
		{
			get => prefabId;
			set => prefabId = value;
		}
		
		[SerializeField] string[] prefabTags = new string[0];

		public string[] PrefabTags
		{
			get => prefabTags ?? new string[0];
			set => prefabTags = (value ?? new string[0]);
		}

#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		public string RoomId { get; set; }
		public string ModelId { get; set; }

		public override void Cleanup()
		{
			base.Cleanup();

			RoomId = null;
			ModelId = null;
		}
	}

}