using Lunra.Hothouse.Models;
using UnityEngine;
using Input = Lunra.Hothouse.Models.Input;
using UnityInput = UnityEngine.Input;

namespace Lunra.Hothouse.Presenters
{
	public class GameInputPresenter : InputPresenter<GameInputModel>
	{
		public GameInputPresenter(GameInputModel input) : base(input)
		{
			Input.Display.Changed += OnDisplay;

			Input.Floor.Changed += v => Debug.Log(v);
		}

		protected override void UnBind()
		{
			base.UnBind();
			
			Input.Display.Changed -= OnDisplay;
		}

		#region InputEvents
		public void OnDisplay(Input.Display display)
		{
			if (!HasCollision(out var hit, LayerMasks.Floor)) return;

			switch (display.State)
			{
				case Models.Input.States.Idle:
					Input.Floor.Value = Models.Input.Generic.Idle(hit.point);
					break;
				case Models.Input.States.Begin:
					Input.Floor.Value = Models.Input.Generic.Begin(hit.point);
					break;
				case Models.Input.States.Active:
					Input.Floor.Value = Input.Floor.Value.NewEnd(
						Models.Input.States.Active,
						hit.point
					);
					break;
				case Models.Input.States.End:
					Input.Floor.Value = Input.Floor.Value.NewEnd(
						Models.Input.States.End,
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