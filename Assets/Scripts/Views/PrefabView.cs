using System;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lunra.Hothouse.Views
{
	public interface IPrefabView : IView
	{
		string PrefabId { get; set; }
		string[] PrefabTags { get; set; }
		string RoomId { get; set; }
		string ModelId { get; set; }
		
#if UNITY_EDITOR
		void CalculateCachedData();
		void NormalizeMeshCollidersFromRoot();
#endif
	}
	
	public class PrefabView : View, IPrefabView//, ICachableView
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
		
		
#if UNITY_EDITOR
		[ContextMenu("Calculate Cached Data")]
		public void CalculateCachedData()
		{
			Undo.RecordObject(this, "Calculate Cached Data");
			PrefabId = name;
			OnCalculateCachedData();
			PrefabUtility.RecordPrefabInstancePropertyModifications(this);
		}

		public void NormalizeMeshCollidersFromRoot()
		{
			NormalizeMeshColliders(RootTransform);
		}

		protected void NormalizeMeshColliders(Transform root)
		{
			foreach (var mesh in root.GetDescendants<MeshFilter>())
			{
				var meshCollider = mesh.GetComponent<MeshCollider>();
				if (meshCollider == null) continue;
				if (meshCollider.sharedMesh != null && meshCollider.sharedMesh == mesh.sharedMesh) continue;
					
				meshCollider.sharedMesh = mesh.sharedMesh;
			}
		}
		
		protected virtual void OnCalculateCachedData() {}
#endif
	}

}