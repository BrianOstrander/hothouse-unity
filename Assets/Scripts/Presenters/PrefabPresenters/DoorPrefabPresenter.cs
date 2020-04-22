using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;
using UnityEngine;

namespace Lunra.WildVacuum.Presenters
{
	public class DoorPrefabPresenter : PrefabPresenter<DoorPrefabView, DoorPrefabModel>
	{
		public DoorPrefabPresenter(GameModel game, DoorPrefabModel prefab) : base(game, prefab)
		{
			Prefab.IsOpen.Changed += OnDoorPrefabIsOpen;
		}

		protected override void OnUnBind()
		{
			base.OnUnBind();
			
			Prefab.IsOpen.Changed -= OnDoorPrefabIsOpen;
		}

		protected override void Show()
		{
			if (View.Visible) return;
			
			base.Show();
			
			if (Prefab.IsOpen.Value) View.Open();
		}

		#region DoorPrefabModel Events
		void OnDoorPrefabIsOpen(bool isOpen)
		{
			if (View.NotVisible) return;
			
			if (isOpen) View.Open();
			else Debug.LogError("Currently no way to re-close a door...");
		}
		#endregion
	}
}