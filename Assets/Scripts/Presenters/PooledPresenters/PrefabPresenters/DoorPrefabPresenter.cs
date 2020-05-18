using System;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class DoorPrefabPresenter : PrefabPresenter<DoorPrefabModel, DoorPrefabView>
	{
		public DoorPrefabPresenter(GameModel game, DoorPrefabModel model) : base(game, model) { }

		protected override void Bind()
		{
			Model.IsOpen.Changed += OnDoorPrefabIsOpen;
			
			base.Bind();
		}
		
		protected override void UnBind()
		{
			Model.IsOpen.Changed -= OnDoorPrefabIsOpen;
			
			base.UnBind();
		}

		protected override void OnViewPrepare()
		{
			if (Model.IsOpen.Value) View.Open();
		}
		
		#region DoorPrefabModel Events
		void OnDoorPrefabIsOpen(bool isOpen)
		{
			if (View.NotVisible) return;
			
			if (isOpen) View.Open();
			else Debug.LogError("Currently no way to re-close a door...");
			
			Game.NavigationMesh.QueueCalculation();
		}
		#endregion
	}
}