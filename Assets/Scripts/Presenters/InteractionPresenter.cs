using System;
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

		protected override void Deconstruct()
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

			var mouseScrollDelta = new Vector3(
				Input.mouseScrollDelta.x,
				Input.mouseScrollDelta.y,
				0f
			);
			
			var scrollIsZero = Mathf.Approximately(0f, mouseScrollDelta.sqrMagnitude);

			switch (Model.Scroll.Value.State)
			{
				case Interaction.States.Idle:
				case Interaction.States.End:
				case Interaction.States.Cancel:
					if (!scrollIsZero)
					{
						Model.Scroll.Value = new Interaction.GenericVector3(
							Interaction.States.Begin,
							Interaction.DeltaVector3.New(
								mouseScrollDelta	
							)
						);
					}
					else
					{
						Model.Scroll.Value = new Interaction.GenericVector3(
							Interaction.States.Idle,
							Interaction.DeltaVector3.New(
								mouseScrollDelta	
							)
						);
					}
					break;
				case Interaction.States.Begin:
				case Interaction.States.Active:
					if (!scrollIsZero)
					{
						Model.Scroll.Value = Model.Scroll.Value.NewEnd(
							Interaction.States.Active,
							mouseScrollDelta
						);
					}
					else
					{
						Model.Scroll.Value = Model.Scroll.Value.NewEnd(
							Interaction.States.End,
							mouseScrollDelta
						);
					}
					break;
				default:
					Debug.LogError("Unrecognized Interaction.State: "+Model.Display.Value.State);
					break;
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
							Interaction.DeltaVector3.New(
								Input.mousePosition
							),
							Interaction.DeltaVector3.New(
								Model.Camera.Value.ScreenToViewportPoint(Input.mousePosition)
							)
						);
					}
					else
					{
						Model.Display.Value = new Interaction.Display(
							Interaction.States.Idle,
							Interaction.DeltaVector3.New(
								Input.mousePosition
							),
							Interaction.DeltaVector3.New(
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