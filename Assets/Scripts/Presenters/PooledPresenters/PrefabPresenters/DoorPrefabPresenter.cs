using System.Linq;
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
			Model.LightSensitive.LightLevel.Changed += OnLightLevel;

			Model.Obligations.All.ChangedSource += OnObligationObligations;
			Model.Enterable.Entrances.Changed += OnEnterableEntrances;

			base.Bind();
		}
		
		protected override void UnBind()
		{
			Game.NavigationMesh.CalculationState.Changed -= OnNavigationMeshCalculationState;
			
			Model.IsOpen.Changed -= OnDoorPrefabIsOpen;
			Model.LightSensitive.LightLevel.Changed -= OnLightLevel;
			
			Model.Obligations.All.ChangedSource -= OnObligationObligations;
			Model.Enterable.Entrances.Changed -= OnEnterableEntrances;
			
			base.UnBind();
		}

		protected override void OnViewPrepare()
		{
			View.IsOpen = Model.IsOpen.Value;

			View.Click += OnViewClick;
			View.Highlight += OnViewHighlight;
			
			Model.RecalculateEntrances(View);
		}

		#region View Events
		void OnViewClick()
		{
			if (Model.IsOpen.Value) return;
			
			if (Model.Obligations.ContainsType(ObligationCategories.Door.Open))
			{
				Model.Obligations.Remove(ObligationCategories.Door.Open);
			}
			else
			{
				Game.ObligationIndicators.Register(
					Obligation.New(
						ObligationCategories.Door.Open,
						0,
						ObligationCategories.GetJobs(Jobs.Construction),
						Obligation.ConcentrationRequirements.Instant,
						Interval.Zero()
					),
					Model
				);
			}
		}

		void OnViewHighlight(bool isHighlighted)
		{
			
		}
		#endregion
		
		#region NavigationMeshModel Events
		void OnNavigationMeshCalculationState(NavigationMeshModel.CalculationStates calculationState)
		{
			if (calculationState != NavigationMeshModel.CalculationStates.Completed) return;
			if (IsNotActive) return;
			
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
		void OnObligationObligations(Obligation[] obligations, object source)
		{
			if (IsNotActive) return;
			if (source == this) return;

			foreach (var obligation in obligations)
			{
				if (obligation.State == Obligation.States.Complete) OnObligationHandle(obligation.Type);
			}
			
			RecalculateObligations();
		}

		void OnObligationHandle(ObligationType type)
		{
			if (!ObligationCategories.Door.Contains(type))
			{
				Debug.LogError("Unrecognized category on this obligation: "+type);
				return;
			}
			
			switch (type.Action)
			{
				case ObligationCategories.Door.Actions.Open:
					if (Model.IsOpen.Value) Debug.LogWarning("Handling obligation \""+type+"\" but the door is already open");
					else
					{
						Model.IsOpen.Value = true;
					}
					break;
				default:
					Debug.LogError("Unrecognized obligation type: "+type);
					break;
			}
		}
		#endregion
		
		#region EnterableModel Events
		void OnEnterableEntrances(Entrance[] entrances)
		{
			if (IsNotActive) return;
			RecalculateObligations();
		}
		#endregion
		
		#region Utility
		void RecalculateObligations()
		{
			var obligations = Model.Obligations.All.Value.ToArray();

			var anyEntranceAvailable = Model.Enterable.Entrances.Value.Any(e => e.State == Entrance.States.Available);
			var anyChanges = false;
			var anyCompleted = false;
			
			for (var i = 0; i < obligations.Length; i++)
			{
				var previousState = obligations[i].State;
				
				switch (obligations[i].State)
				{
					case Obligation.States.NotInitialized:
					case Obligation.States.Blocked:
					case Obligation.States.Available:
						obligations[i] = obligations[i].New(
							anyEntranceAvailable ? Obligation.States.Available : Obligation.States.Blocked 
						);
						break;
					case Obligation.States.Promised:
						if (!anyEntranceAvailable)
						{
							// Something is blocking this door, or its no longer lit, so anyone trying to go to it
							// should lose track of this obligation, which is why we block it AND change the PromiseId.
							obligations[i] = obligations[i]
								.New(Obligation.States.Blocked)
								.NewPromiseId();
						}
						break;
					case Obligation.States.Complete:
						anyCompleted = true;
						break;
					default:
						Debug.LogError("Unrecognized State: "+obligations[i].State);
						break;
				}

				anyChanges |= previousState != obligations[i].State;
			}

			if (!anyChanges && !anyCompleted) return;

			Model.Obligations.All.SetValue(
				anyCompleted ? obligations.Where(o => o.State != Obligation.States.Complete).ToArray() : obligations,
				this
			);
		}
		#endregion
	}
}