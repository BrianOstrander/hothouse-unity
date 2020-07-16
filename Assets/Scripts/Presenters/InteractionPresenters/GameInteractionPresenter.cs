using System;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class GameInteractionPresenter : InteractionPresenter<GameInteractionModel>
	{
		public GameInteractionPresenter(GameInteractionModel model) : base(model)
		{
			Model.Display.Changed += OnDisplay;

			// Model.RadialFloorSelection.Changed += v => Debug.Log(v);
		}

		protected override void UnBind()
		{
			base.UnBind();
			
			Model.Display.Changed -= OnDisplay;
		}

		protected override void UpdateInteractions()
		{
			base.UpdateInteractions();

			Vector3? pan = null;
			
			if (Input.GetKey(KeyCode.Comma)) pan = View.transform.forward;
			if (Input.GetKey(KeyCode.O)) pan = (pan ?? Vector3.zero) - View.transform.forward;
			if (Input.GetKey(KeyCode.A)) pan = (pan ?? Vector3.zero) - View.transform.right;
			if (Input.GetKey(KeyCode.E)) pan = (pan ?? Vector3.zero) + View.transform.right;

			if (pan.HasValue)
			{
				switch (Model.CameraPan.Value.State)
				{
					case Interaction.States.End:
					case Interaction.States.Idle:
						Model.CameraPan.Value = Interaction.GenericVector3.Point(
							Interaction.States.Begin,
							pan.Value
						);
						break;
					case Interaction.States.Begin:
					case Interaction.States.Active:
						Model.CameraPan.Value = Model.CameraPan.Value.NewEnd(
							Interaction.States.Active,
							pan.Value
						);
						break;
					default:
						Debug.LogError("Unexpected State: " + Model.CameraPan.Value.State);
						break;
				}
			}
			else
			{
				switch (Model.CameraPan.Value.State)
				{
					case Interaction.States.Idle:
						break;
					case Interaction.States.End:
						Model.CameraPan.Value = Interaction.GenericVector3.Point(Interaction.States.Idle, Vector3.zero);
						break;
					case Interaction.States.Begin:
					case Interaction.States.Active:
						Model.CameraPan.Value = Model.CameraPan.Value.NewState(Interaction.States.End);
						break;
					default:
						Debug.LogError("Unexpected State: "+Model.CameraPan.Value.State);
						break;
				}
			}
			
			float? orbit = null;
			
			if (Input.GetKey(KeyCode.Quote)) orbit = 1f;
			if (Input.GetKey(KeyCode.Period)) orbit = (orbit ?? 0f) - 1f;
			
			if (orbit.HasValue)
			{
				switch (Model.CameraOrbit.Value.State)
				{
					case Interaction.States.End:
					case Interaction.States.Idle:
						Model.CameraOrbit.Value = Interaction.GenericFloat.Point(
							Interaction.States.Begin,
							orbit.Value
						);
						break;
					case Interaction.States.Begin:
					case Interaction.States.Active:
						Model.CameraOrbit.Value = Model.CameraOrbit.Value.NewEnd(
							Interaction.States.Active,
							orbit.Value
						); 
						break;
					default:
						Debug.LogError("Unexpected State: " + Model.CameraOrbit.Value.State);
						break;
				}
			}
			else
			{
				switch (Model.CameraOrbit.Value.State)
				{
					case Interaction.States.Idle:
						break;
					case Interaction.States.End:
						Model.CameraOrbit.Value = Interaction.GenericFloat.Point(Interaction.States.Idle, 0f);
						break;
					case Interaction.States.Begin:
					case Interaction.States.Active:
						Model.CameraOrbit.Value = Model.CameraOrbit.Value.NewState(Interaction.States.End);
						break;
					default:
						Debug.LogError("Unexpected State: "+Model.CameraPan.Value.State);
						break;
				}
			}
			
			if (pan.HasValue || orbit.HasValue) OnDisplay(Model.Display.Value);
		}

		#region InputEvents
		public void OnDisplay(Interaction.Display display)
		{
			switch (display.State)
			{
				case Interaction.States.Idle:
				case Interaction.States.Begin:
					if (HasRoomCollision(out var hit, out var hitRoomId))
					{
						Model.FloorSelection.Value = Interaction.RoomVector3.Point(
							display.State,
							hitRoomId,
							hit.point
						);
					}
					else
					{
						Model.FloorSelection.Value = Interaction.RoomVector3.Point(
							Interaction.States.OutOfRange,
							null,
							Vector3.zero
						);
					}
					break;
				case Interaction.States.Active:
					if (
						Model.FloorSelection.Value.State != Interaction.States.Active &&
						Model.FloorSelection.Value.State != Interaction.States.Begin
					) break;

					if (HasCollision(out var hitPositionActive, new Plane(Vector3.up, Model.FloorSelection.Value.Value.Begin)))
					{
						Model.FloorSelection.Value = Model.FloorSelection.Value.NewEnd(
							display.State,
							hitPositionActive
						);
					}
					
					break;
				case Interaction.States.End:
				case Interaction.States.Cancel:
					if (Model.FloorSelection.Value.State != Interaction.States.Active) break;
					
					if (HasCollision(out var hitPositionEndOrCancel, new Plane(Vector3.up, Model.FloorSelection.Value.Value.Begin)))
					{
						Model.FloorSelection.Value = Model.FloorSelection.Value.NewEnd(
							display.State,
							hitPositionEndOrCancel
						);
					}
					else
					{
						Model.FloorSelection.Value = Model.FloorSelection.Value.NewState(display.State);
					}
					
					break;
				default:
					Debug.LogError("Unrecognized State: "+display.State);
					break;
			}
		}
		#endregion

		protected bool HasRoomCollision(
			out RaycastHit hit,
			out string roomId
		)
		{
			roomId = null;
			if (!HasCollision(out hit, LayerMasks.Floor)) return false;

			var result = hit.transform.GetAncestor<View>(v => v is IRoomIdView) as IRoomIdView;

			if (result == null) return false;

			roomId = result.RoomId;
			return true;
		}
	}
}