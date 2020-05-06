using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;
using UnityEngine;

namespace Lunra.WildVacuum.Presenters
{
	public class DoorPrefabPresenter : PrefabPresenter<DoorPrefabModel, DoorPrefabView>
	{
		public DoorPrefabPresenter(GameModel game, DoorPrefabModel model) : base(game, model) { }

		protected override void OnBind()
		{
			base.OnBind();
			Model.IsOpen.Changed += OnDoorPrefabIsOpen;
		}

		protected override void OnUnBind()
		{
			base.OnUnBind();
			Model.IsOpen.Changed -= OnDoorPrefabIsOpen;
		}

		protected override void OnShow()
		{
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