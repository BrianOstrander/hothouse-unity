using System;
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

			Model.Obligations.ChangedSource += OnObligationObligations;
			Model.Entrances.Changed += OnEnterableEntrances;

			base.Bind();
		}
		
		protected override void UnBind()
		{
			Game.NavigationMesh.CalculationState.Changed -= OnNavigationMeshCalculationState;
			
			Model.IsOpen.Changed -= OnDoorPrefabIsOpen;
			Model.LightSensitive.LightLevel.Changed -= OnLightLevel;
			
			Model.Obligations.ChangedSource -= OnObligationObligations;
			Model.Entrances.Changed -= OnEnterableEntrances;
			
			base.UnBind();
		}

		protected override void OnViewPrepare()
		{
			if (Model.IsOpen.Value) View.Open();
			
			Model.RecalculateEntrances(View);
		}
		
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
			
			if (View.NotVisible) return;
			
			if (isOpen) View.Open();
			else Debug.LogError("Currently no way to re-close a door...");
			
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
				if (obligation.State != Obligation.States.Complete) return;
				OnObligationHandle(obligation.Type);
			}
			
			RecalculateObligations();
		}

		void OnObligationHandle(string type)
		{
			switch (type)
			{
				case ObligationTypes.Door.Open:
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
			var obligations = Model.Obligations.Value.ToArray();

			var anyEntranceAvailable = Model.Entrances.Value.Any(e => e.State == Entrance.States.Available);
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
							// should lose track of this obligation, which is why we block it AND change the Id.
							obligations[i] = obligations[i]
								.New(Obligation.States.Blocked)
								.NewId();
							
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

			Model.Obligations.SetValue(
				anyCompleted ? obligations.Where(o => o.State != Obligation.States.Complete).ToArray() : obligations,
				this
			);
		}
		#endregion
	}
}