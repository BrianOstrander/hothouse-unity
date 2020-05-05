using System;
using Lunra.Core;
using Lunra.StyxMvp;
using Lunra.WildVacuum.Models;
using UnityEngine;

namespace Lunra.WildVacuum.Views
{
	public class ItemDropView : View
	{
		[Serializable]
		struct ItemEntry
		{
			public Item.Types Type;
			public Mesh Mesh;
			public Material Material;
		}
		
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null

		[SerializeField] MeshFilter meshFilter;
		[SerializeField] MeshRenderer meshRenderer;
		[SerializeField] ItemEntry itemEntryDefault;
		[SerializeField] ItemEntry[] itemEntries;
		
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		public void SetEntry(int count, Item.Types type)
		{
			var entry = itemEntries.FirstOrFallback(e => e.Type == type, itemEntryDefault);

			meshFilter.mesh = entry.Mesh;
			meshRenderer.material = entry.Material;
		}

		public override void Reset()
		{
			base.Reset();
			
			SetEntry(0, Item.Types.Unknown);
		}
	}

}