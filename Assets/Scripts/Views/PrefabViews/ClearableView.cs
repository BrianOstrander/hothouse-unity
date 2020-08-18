using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public interface IClearableView : IPrefabView, IEnterableView
	{
		void Highlight();
		void Select();
		void Deselect();
		
		float MeleeRangeBonus { get; }
	}
	
	public class ClearableView : PrefabView, IClearableView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] GameObject entrancesRoot;
		[SerializeField] Transform[] entrances = new Transform[0];
		[SerializeField] float meleeRangeBonus;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public virtual void Highlight() => SetColor(color => color.NewV(0.5f));
		public virtual void Select() => SetColor(color => color.NewV(0.75f));
		public virtual void Deselect() => SetColor(color => color.NewV(1f));
		#endregion
		
		#region Reverse Bindings
		public float MeleeRangeBonus => meleeRangeBonus;

		public GameObject EntrancesRoot
		{
			get => entrancesRoot;
			set => entrancesRoot = value;
		}

		public Transform[] Entrances
		{
			get => entrances;
			set => entrances = value;
		}
		#endregion

		public override void Cleanup()
		{
			base.Cleanup();

			Deselect();
		}

		#region Utility
		protected void SetColor(Func<Color, Color> apply)
		{
			foreach (var renderer in gameObject.GetComponentsInChildren<MeshRenderer>()) renderer.material.color = apply(renderer.material.color);
		}
		#endregion

#if UNITY_EDITOR
		protected override void OnCalculateCachedData()
		{
			NormalizeMeshCollidersFromRoot();

			this.CalculateCachedEntrances();
		}

		void OnDrawGizmosSelected()
		{
			if (Application.isPlaying) return;
			
			Gizmos.color = Color.green;
			foreach (var entrance in entrances) Gizmos.DrawWireCube(entrance.position, Vector3.one * 0.1f);

			// if (Mathf.Approximately(0f, MeleeRangeBonus)) return;
			//
			// Handles.color = Color.green.NewA(0.3f);
			// Handles.DrawWireDisc(transform.position, Vector3.up, meleeRangeBonus);
		}
#endif
	}

}