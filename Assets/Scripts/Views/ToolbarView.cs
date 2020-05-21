using System;
using Lunra.StyxMvp;
using UnityEngine;
using UnityEngine.UI;

namespace Lunra.Hothouse.Views
{
	public class ToolbarView : View
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] Color selectedColor;
		[SerializeField] Color notSelectedColor;
		
		[SerializeField] Graphic clearanceGraphic;
		[SerializeField] Graphic constructFireGraphic;
		[SerializeField] Graphic constructBedGraphic;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public event Action ClearanceClick;
		public event Action ConstructFireClick;
		public event Action ConstructBedClick;

		public bool ClearanceSelected { set => clearanceGraphic.color = value ? selectedColor : notSelectedColor; }
		public bool ConstructFireSelected { set => constructFireGraphic.color = value ? selectedColor : notSelectedColor; }
		public bool ConstructBedSelected { set => constructBedGraphic.color = value ? selectedColor : notSelectedColor; }
		#endregion

		public override void Reset()
		{
			base.Reset();

			ClearanceClick = null;
			ConstructFireClick = null;
			ConstructBedClick = null;

			ClearanceSelected = false;
			ConstructFireSelected = false;
			ConstructBedSelected = false;
		}

		#region Events
		public void OnClearanceClick() => ClearanceClick?.Invoke();
		public void OnConstructFireClick() => ConstructFireClick?.Invoke();
		public void OnConstructBedClick() => ConstructBedClick?.Invoke();
		#endregion
	}

}