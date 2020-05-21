using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class InputPresenter<M> : Presenter<InteractionView>
		where M : InteractionModel
	{
		protected M Model { get; private set; }

		public InputPresenter(
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
		void OnHeartbeatUpdate() => UpdateInputs();
		#endregion

		protected virtual void UpdateInputs()
		{
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
			else if (Input.GetMouseButton(0))
			{
				Model.Display.Value = Model.Display.Value.NewEnds(
					Interaction.States.Active,
					Input.mousePosition,
					Model.Camera.Value.ScreenToViewportPoint(Input.mousePosition)
				);
				
				// TODO: Add ability to cancel here!
			}
			else if (Input.GetMouseButtonUp(0))
			{
				Model.Display.Value = Model.Display.Value.NewEnds(
					Interaction.States.End,
					Input.mousePosition,
					Model.Camera.Value.ScreenToViewportPoint(Input.mousePosition)
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
		#endregion
	}
}