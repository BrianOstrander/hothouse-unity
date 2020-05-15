using System;
using Lunra.Core;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public interface IClearableView : IPrefabView
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
		#endregion

		public override void Reset()
		{
			base.Reset();

			Deselect();
		}

		#region Utility
		protected void SetColor(Func<Color, Color> apply)
		{
			foreach (var renderer in gameObject.GetComponentsInChildren<MeshRenderer>()) renderer.material.color = apply(renderer.material.color);
		}
		#endregion

		void OnDrawGizmosSelected()
		{
			if (Mathf.Approximately(0f, MeleeRangeBonus)) return;
			
			Handles.color = Color.green.NewA(0.3f);
			Handles.DrawWireDisc(transform.position, Vector3.up, meleeRangeBonus);
		}
	}

}