using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class DoorPresenter : PrefabPresenter<DoorModel, DoorView>
	{
		RoomModel room0;
		RoomModel room1;

		public DoorPresenter(GameModel game, DoorModel model) : base(game, model) { }
		
		protected override void Bind()
		{
			room0 = Game.Rooms.FirstActive(Model.RoomConnection.Value.RoomId0);
			room1 = Game.Rooms.FirstActive(Model.RoomConnection.Value.RoomId1);
			
			Game.NavigationMesh.CalculationState.Changed += OnNavigationMeshCalculationState;

			room0.IsRevealed.Changed += OnRoomIsRevealed;
			room1.IsRevealed.Changed += OnRoomIsRevealed;
			
			Model.IsOpen.Changed += OnDoorPrefabIsOpen;
			Model.LightSensitive.LightLevel.Changed += OnLightLevel;

			Model.Obligations.Bind(ObligationCategories.Door.Open, OnObligationDoorOpen);

			base.Bind();
		}
		
		protected override void UnBind()
		{
			Game.NavigationMesh.CalculationState.Changed -= OnNavigationMeshCalculationState;
			
			room0.IsRevealed.Changed -= OnRoomIsRevealed;
			room1.IsRevealed.Changed -= OnRoomIsRevealed;
			
			Model.IsOpen.Changed -= OnDoorPrefabIsOpen;
			Model.LightSensitive.LightLevel.Changed -= OnLightLevel;
			
			Model.Obligations.UnBind(ObligationCategories.Door.Open, OnObligationDoorOpen);
			
			base.UnBind();
		}

		protected override bool AutoShowCloseOnRoomReveal => false;

		protected override void OnSimulationInitialized()
		{
			OnRoomIsRevealed(CanShow());
		}

		protected override void OnViewPrepare()
		{
			base.OnViewPrepare();
			
			View.IsOpen = Model.IsOpen.Value;
			View.RoomId = Model.RoomTransform.Id.Value;

			View.Click += OnViewClick;
			View.Highlight += OnViewHighlight;
			
			Model.RecalculateEntrances(View);
		}

		#region View Events
		void OnViewClick()
		{
			if (Model.IsOpen.Value) return;

			var openDoorObligation = ObligationCategories.Door.Open;
			
			if (Model.Obligations.HasAny(openDoorObligation)) Model.Obligations.RemoveAny(openDoorObligation);
			else Model.Obligations.Add(openDoorObligation);
		}

		void OnViewHighlight(bool isHighlighted)
		{
			
		}
		#endregion
		
		#region PooledModel Events
		protected override bool CanShow() => room0.IsRevealed.Value || room1.IsRevealed.Value;
		#endregion
		
		#region RoomModel Events
		void OnRoomIsRevealed(bool isRevealed)
		{
			if (isRevealed) Show();
			else Close();
		}
		#endregion
		
		#region NavigationMeshModel Events
		void OnNavigationMeshCalculationState(NavigationMeshModel.CalculationStates calculationState)
		{
			if (calculationState != NavigationMeshModel.CalculationStates.Completed) return;
			
			Model.RecalculateEntrances();
		}
		#endregion
		
		#region DoorPrefabModel Events
		void OnDoorPrefabIsOpen(bool isOpen)
		{
			Game.LastLightUpdate.Value = Game.LastLightUpdate.Value.SetRoomStale(
				Model.RoomConnection.Value.RoomId0,
				Model.RoomConnection.Value.RoomId1	
			);

			void triggerRoomUpdate(string roomId)
			{
				var room = Game.Rooms.FirstOrDefaultActive(roomId);
				var otherRoomId = roomId == Model.RoomConnection.Value.RoomId0 ? Model.RoomConnection.Value.RoomId1 : Model.RoomConnection.Value.RoomId0; 
				
				if (room == null)
				{
					Debug.LogError("Unable to find a roomId matching: "+roomId);
					return;
				}

				room.UpdateConnection(otherRoomId, Model.IsOpen.Value);
			}

			triggerRoomUpdate(Model.RoomConnection.Value.RoomId0);
			triggerRoomUpdate(Model.RoomConnection.Value.RoomId1);
			
			if (View.NotVisible) return;

			View.IsOpen = isOpen;
			
			Game.NavigationMesh.QueueCalculation();
		}

		void OnLightLevel(float lightLevel)
		{
			Model.RecalculateEntrances();
		}
		#endregion
		
		#region ObligationModel Events
		void OnObligationDoorOpen(Obligation obligation, IModel source)
		{
			if (Model.IsOpen.Value) Debug.LogWarning("Handling obligation \""+obligation.Type+"\" but the door is already open");
			else Model.IsOpen.Value = true;
		}
		#endregion
	}
}