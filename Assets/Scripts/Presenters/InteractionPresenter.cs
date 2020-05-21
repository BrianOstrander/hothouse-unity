using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class InteractionPresenter<M> : Presenter<InteractionView>
		where M : InteractionModel
	{
		protected M Model { get; private set; }

		public InteractionPresenter(
			M model
		)
		{
			Model = model;

			App.Heartbeat.Update += OnHeartbeatUpdate;
			
			ShowView(instant: true);
		}

		protected override void UnBind()
		{
			App.Heartbeat.Update -= OnHeartbeatUpdate;
		}
		
		#region Heartbeat Events
		void OnHeartbeatUpdate() => UpdateInteractions();
		#endregion

		protected virtual void UpdateInteractions()
		{
			if (Input.GetKeyUp(KeyCode.Escape))
			{
				if (Model.Display.Value.State == Interaction.States.Active)
				{
					Model.Display.Value = Model.Display.Value.NewEnds(
						Interaction.States.Cancel,
						Input.mousePosition,
						Model.Camera.Value.ScreenToViewportPoint(Input.mousePosition)	
					);
				}
			}

			switch (Model.Display.Value.State)
			{
				case Interaction.States.Idle:
				case Interaction.States.End:
				case Interaction.States.Cancel:
					if (Input.GetMouseButtonDown(0))
					{
						Model.Display.Value = new Interaction.Display(
							Interaction.States.Begin,
							Interaction.Vector3Delta.Point(
								Input.mousePosition
							),
							Interaction.Vector3Delta.Point(
								Model.Camera.Value.ScreenToViewportPoint(Input.mousePosition)
							)
						);
					}
					else
					{
						Model.Display.Value = new Interaction.Display(
							Interaction.States.Idle,
							Interaction.Vector3Delta.Point(
								Input.mousePosition
							),
							Interaction.Vector3Delta.Point(
								Model.Camera.Value.ScreenToViewportPoint(Input.mousePosition)
							)
						);
					}
					break;
				case Interaction.States.Begin:
				case Interaction.States.Active:
					if (Input.GetMouseButton(0))
					{
						Model.Display.Value = Model.Display.Value.NewEnds(
							Interaction.States.Active,
							Input.mousePosition,
							Model.Camera.Value.ScreenToViewportPoint(Input.mousePosition)
						);
					}
					else if (Input.GetMouseButtonUp(0))
					{
						Model.Display.Value = Model.Display.Value.NewEnds(
							Interaction.States.End,
							Input.mousePosition,
							Model.Camera.Value.ScreenToViewportPoint(Input.mousePosition)
						);
					}
					break;
				default:
					Debug.LogError("Unrecognized Interaction.State: "+Model.Display.Value.State);
					break;
			}
		}

		#region Utility
		protected Ray CurrentRay => Model.Camera.Value.ScreenPointToRay(Input.mousePosition);
		protected bool HasCollision(
			out RaycastHit hit,
			int layerMask
		)
		{
			return Physics.Raycast(
				CurrentRay,
				out hit,
				float.MaxValue,
				layerMask
			);
		}
		
		protected bool HasCollision(
			out Vector3 hitPosition,
			Plane plane
		)
		{
			hitPosition = Vector3.zero;
			var ray = CurrentRay;
			if (plane.Raycast(ray, out var distance))
			{
				hitPosition = ray.origin + (ray.direction * distance);
				return true;
			}

			return false;
		}
		#endregion
	}
}