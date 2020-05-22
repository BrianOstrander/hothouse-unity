using System;
using Lunra.Hothouse.Models;
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

		#region InputEvents
		public void OnDisplay(Interaction.Display display)
		{
			switch (display.State)
			{
				case Interaction.States.Idle:
				case Interaction.States.Begin:
					if (HasCollision(out var hit, LayerMasks.Floor))
					{
						Model.FloorSelection.Value = Interaction.Generic.Point(
							display.State,
							hit.point
						);
					}
					else
					{
						Model.FloorSelection.Value = Interaction.Generic.Point(
							Interaction.States.OutOfRange,
							Vector3.zero
						);
					}
					break;
				case Interaction.States.Active:
					if (
						Model.FloorSelection.Value.State != Interaction.States.Active &&
						Model.FloorSelection.Value.State != Interaction.States.Begin
					) break;

					if (HasCollision(out var hitPositionActive, new Plane(Vector3.up, Model.FloorSelection.Value.Position.Begin)))
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
					
					if (HasCollision(out var hitPositionEndOrCancel, new Plane(Vector3.up, Model.FloorSelection.Value.Position.Begin)))
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
			
			/*
			if (!HasCollision(out var hit, LayerMasks.Floor))
			{
				switch (Model.RadialFloorSelection.Value.State)
				{
					case Interaction.States.OutOfRange:
						return;
					case Interaction.States.Idle:
					case Interaction.States.Begin:
					case Interaction.States.Active:
						Model.RadialFloorSelection.Value = Model.RadialFloorSelection.Value.NewState(
							Interaction.States.OutOfRange
						);
						return;
					case Interaction.States.End:
					case Interaction.States.Cancel:
						Model.RadialFloorSelection.Value = Model.RadialFloorSelection.Value.NewState(
							Interaction.States.Cancel
						);
						return;
					default:
						Debug.LogError("Unrecognized Interaction.State: "+Model.RadialFloorSelection.Value.State);
						return;
				}
			}

			// These checks occur so we're sure we started dragging a valid selection.
			switch (display.State)
			{
				case Interaction.States.Idle:
				case Interaction.States.Begin:
					Model.RadialFloorSelection.Value = Interaction.Generic.Point(
						display.State,
						hit.point
					);
					break;
				case Interaction.States.Active:
					if (
						Model.RadialFloorSelection.Value.State != Interaction.States.Begin &&
						Model.RadialFloorSelection.Value.State != Interaction.States.Active &&
						Model.RadialFloorSelection.Value.State != Interaction.States.OutOfRange
					) break;
					
					Model.RadialFloorSelection.Value = Model.RadialFloorSelection.Value.NewEnd(
						display.State,
						hit.point
					);
					break;
				case Interaction.States.End:
				case Interaction.States.Cancel:
					if (Model.RadialFloorSelection.Value.State != Interaction.States.Active) break;
					
					Model.RadialFloorSelection.Value = Model.RadialFloorSelection.Value.NewEnd(
						display.State,
						hit.point
					);
					break;
				default:
					Debug.LogError("Unrecognized State: "+display.State);
					break;
			}
			*/
		}
		#endregion
	}
}