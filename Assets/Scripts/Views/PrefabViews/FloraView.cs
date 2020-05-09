using System;
using Lunra.Core;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class FloraView : PrefabView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		
		[SerializeField] float maximumHeight;
		[SerializeField] Transform heightRoot;
		
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public float Age { set => heightRoot.localPosition = new Vector3(0f, maximumHeight * Mathf.Clamp01(value), 0f); }

		public bool IsReproducing
		{
			set
			{
				var saturation = value ? 0.47f : 0.37f;
				SetColor(color => color.NewS(saturation));
			}
		}

		public void Highlight() => SetColor(color => color.NewV(0.5f));
		public void Select() => SetColor(color => color.NewV(0.75f));
		public void Deselect() => SetColor(color => color.NewV(1f));
		#endregion
		
		#region Reverse Bindings
		#endregion

		public override void Reset()
		{
			base.Reset();

			Age = 0f;
			IsReproducing = false;
			Deselect();
		}

		#region Utility
		void SetColor(Func<Color, Color> apply)
		{
			foreach (var renderer in gameObject.GetComponentsInChildren<MeshRenderer>()) renderer.material.color = apply(renderer.material.color);
		}
		#endregion
	}

}