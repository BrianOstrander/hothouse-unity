using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;
using UnityEngine;

namespace Lunra.WildVacuum.Presenters
{
	public class DoorPrefabPresenter : PrefabPresenter<DoorPrefabView, DoorPrefabModel>
	{
		public DoorPrefabPresenter(GameModel game, DoorPrefabModel model) : base(game, model)
		{
			Model.IsOpen.Changed += OnDoorPrefabIsOpen;
		}

		protected override void OnUnBind()
		{
			base.OnUnBind();
			
			Model.IsOpen.Changed -= OnDoorPrefabIsOpen;
		}

		protected override void Show()
		{
			if (View.Visible) return;
			
			base.Show();
			
			if (Model.IsOpen.Value) View.Open();
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