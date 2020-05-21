using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class GameInteractionPresenter : InputPresenter<GameInteractionModel>
	{
		public GameInteractionPresenter(GameInteractionModel model) : base(model)
		{
			Model.Display.Changed += OnDisplay;

			// Model.Floor.Changed += v => Debug.Log(v);
		}

		protected override void UnBind()
		{
			base.UnBind();
			
			Model.Display.Changed -= OnDisplay;
		}

		#region InputEvents
		public void OnDisplay(Interaction.Display display)
		{
			if (!HasCollision(out var hit, LayerMasks.Floor)) return;

			switch (display.State)
			{
				case Interaction.States.Idle:
					Model.Floor.Value = Interaction.Generic.Idle(hit.point);
					break;
				case Interaction.States.Begin:
					Model.Floor.Value = Interaction.Generic.Begin(hit.point);
					break;
				case Interaction.States.Active:
					Model.Floor.Value = Model.Floor.Value.NewEnd(
						Interaction.States.Active,
						hit.point
					);
					break;
				case Interaction.States.End:
					Model.Floor.Value = Model.Floor.Value.NewEnd(
						Interaction.States.End,
						hit.point
					);
					break;
				default:
					Debug.LogError("Unrecognized State: "+display.State);
					break;
			}
		}
		#endregion
	}
}