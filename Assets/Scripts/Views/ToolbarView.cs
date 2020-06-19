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
		[SerializeField] Graphic constructWallGraphic;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public event Action ClearanceClick;
		public event Action ConstructFireClick;
		public event Action ConstructBedClick;
		public event Action ConstructWallClick;

		public bool ClearanceSelected { set => clearanceGraphic.color = value ? selectedColor : notSelectedColor; }
		public bool ConstructFireSelected { set => constructFireGraphic.color = value ? selectedColor : notSelectedColor; }
		public bool ConstructBedSelected { set => constructBedGraphic.color = value ? selectedColor : notSelectedColor; }
		public bool ConstructWallSelected { set => constructWallGraphic.color = value ? selectedColor : notSelectedColor; }
		#endregion

		public override void Cleanup()
		{
			base.Cleanup();

			ClearanceClick = null;
			ConstructFireClick = null;
			ConstructBedClick = null;
			ConstructWallClick = null;

			ClearanceSelected = false;
			ConstructFireSelected = false;
			ConstructBedSelected = false;
			ConstructWallSelected = false;
		}

		#region Events
		public void OnClearanceClick() => ClearanceClick?.Invoke();
		public void OnConstructFireClick() => ConstructFireClick?.Invoke();
		public void OnConstructBedClick() => ConstructBedClick?.Invoke();
		public void OnConstructWallClick() => ConstructWallClick?.Invoke();
		#endregion
	}

}