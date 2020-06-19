using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class ItemDropView : PrefabView
	{
		[Serializable]
		struct ItemEntry
		{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
			public Inventory.Types Type;
			public Mesh Mesh;
			public Material Material;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		}
		
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null

		[SerializeField] MeshFilter meshFilter;
		[SerializeField] MeshRenderer meshRenderer;
		[SerializeField] ItemEntry itemEntryDefault;
		[SerializeField] ItemEntry[] itemEntries;
		
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		public void SetEntry(int count, Inventory.Types type)
		{
			var entry = itemEntries.FirstOrFallback(e => e.Type == type, itemEntryDefault);

			meshFilter.mesh = entry.Mesh;
			meshRenderer.material = entry.Material;
		}

		public override void Cleanup()
		{
			base.Cleanup();
			
			SetEntry(0, Inventory.Types.Unknown);
		}
	}

}