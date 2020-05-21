using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;
using UnityEngine;
using Input = Lunra.Hothouse.Models.Input;
using UnityInput = UnityEngine.Input;

namespace Lunra.Hothouse.Presenters
{
	public class InputPresenter<M> : Presenter<InputView>
		where M : InputModel
	{
		protected InputModel Input { get; private set; }

		public InputPresenter(
			InputModel input
		)
		{
			Input = input;

			App.Heartbeat.Update += OnHeartbeatUpdate;
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
			if (UnityInput.GetMouseButtonDown(0))
			{
				Input.Display.Value = new Input.Display(
					Models.Input.States.Begin,
					Models.Input.Vector3Delta.Point(
						UnityInput.mousePosition
					),
					Models.Input.Vector3Delta.Point(
						Input.Camera.Value.ScreenToViewportPoint(UnityInput.mousePosition)
					)
				);
			}
			else if (UnityInput.GetMouseButton(0))
			{
				Input.Display.Value = Input.Display.Value.NewEnds(
					Models.Input.States.Active,
					UnityInput.mousePosition,
					Input.Camera.Value.ScreenToViewportPoint(UnityInput.mousePosition)
				);
			}
			else if (UnityInput.GetMouseButtonUp(0))
			{
				Input.Display.Value = Input.Display.Value.NewEnds(
					Models.Input.States.End,
					UnityInput.mousePosition,
					Input.Camera.Value.ScreenToViewportPoint(UnityInput.mousePosition)
				);
			}
			else
			{
				Input.Display.Value = new Input.Display(
					Models.Input.States.Idle,
					Models.Input.Vector3Delta.Point(
						UnityInput.mousePosition
					),
					Models.Input.Vector3Delta.Point(
						Input.Camera.Value.ScreenToViewportPoint(UnityInput.mousePosition)
					)
				);
			}
		}

		#region Utility
		protected Ray CurrentRay => Input.Camera.Value.ScreenPointToRay(UnityInput.mousePosition);
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