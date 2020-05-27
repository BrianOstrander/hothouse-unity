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
			Game.NavigationMesh.CalculationState.Changed += OnNavigationMeshCalculationState;
			
			Model.IsOpen.Changed += OnDoorPrefabIsOpen;
			Model.LightLevel.Changed += OnLightLevel;

			base.Bind();
		}
		
		protected override void UnBind()
		{
			Game.NavigationMesh.CalculationState.Changed -= OnNavigationMeshCalculationState;
			
			Model.IsOpen.Changed -= OnDoorPrefabIsOpen;
			Model.LightLevel.Changed -= OnLightLevel;
			
			base.UnBind();
		}

		protected override void OnViewPrepare()
		{
			if (Model.IsOpen.Value) View.Open();
			
			Model.Recalculate(View);
		}
		
		#region NavigationMeshModel Events
		void OnNavigationMeshCalculationState(NavigationMeshModel.CalculationStates calculationState)
		{
			if (calculationState != NavigationMeshModel.CalculationStates.Completed) return;
			if (IsNotActive) return;
			
			Model.Recalculate();
		}
		#endregion
		
		#region DoorPrefabModel Events
		void OnDoorPrefabIsOpen(bool isOpen)
		{
			Game.LastLightUpdate.Value = Game.LastLightUpdate.Value.SetRoomStale(
				Model.RoomConnection.Value.RoomId0,
				Model.RoomConnection.Value.RoomId1	
			); 
			
			if (View.NotVisible) return;
			
			if (isOpen) View.Open();
			else Debug.LogError("Currently no way to re-close a door...");
			
			Game.NavigationMesh.QueueCalculation();
		}

		void OnLightLevel(float lightLevel)
		{
			Model.Recalculate();
		}
		#endregion
	}
}