using System.Linq;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;
using UnityEngine;
using UnityInput = UnityEngine.Input;

namespace Lunra.Hothouse.Presenters
{
	public class CursorPresenter : Presenter<CursorView>
	{
		GameModel game;
		CursorModel cursor;

		public CursorPresenter(
			GameModel game
		)
		{
			this.game = game;
			cursor = game.Cursor;

			game.SimulationInitialize += OnGameSimulationInitialize;
			
			cursor.Current.Changed += OnCursorCurrent;

			App.Heartbeat.Update += OnHeartbeatUpdate;
		}

		protected override void UnBind()
		{
			game.SimulationInitialize -= OnGameSimulationInitialize;
			
			cursor.Current.Changed -= OnCursorCurrent;
			
			App.Heartbeat.Update -= OnHeartbeatUpdate;
		}
		
		#region Heartbeat Events
		void OnHeartbeatUpdate()
		{
			if (UnityInput.GetMouseButtonDown(0)) OnInputMousePrimaryDown();
			else if (UnityInput.GetMouseButton(0)) OnInputMousePrimaryPressed();
			else if (UnityInput.GetMouseButtonUp(0)) OnInputMousePrimaryUp();
			
			// var hits = Physics.RaycastAll(
			// 	game.WorldCamera.CameraInstance.Value.ScreenPointToRay(Input.mousePosition),
			// 	game.WorldCamera.CameraInstance.Value.farClipPlane
			// );
			//
			// foreach (var hit in hits) Debug.Log(hit.transform.name);
			//
			// if (hits.Any()) Debug.Log("-----");
		}
		#endregion
		
		#region Input Events
		void OnInputMousePrimaryDown()
		{
			if (!HasCollision(out var hit)) return;
			
			cursor.Current.Value = CursorModel.Selection.Highlighting(hit.point, hit.point);
		}
		
		void OnInputMousePrimaryPressed()
		{
			if (cursor.Current.Value.State != CursorModel.States.Highlighting) return;

			var currentRay = CurrentRay;
			if (!cursor.Current.Value.Surface.Raycast(currentRay, out var surfaceDistance)) return;
			
			cursor.Current.Value = CursorModel.Selection.Highlighting(
				cursor.Current.Value.Begin,
				currentRay.origin + (currentRay.direction * surfaceDistance)
			);
		}
		
		void OnInputMousePrimaryUp()
		{
			if (cursor.Current.Value.State != CursorModel.States.Highlighting) return;

			cursor.Current.Value = cursor.Current.Value.NewState(CursorModel.States.Selected);
		}
		#endregion
		
		#region GameModel Events
		void OnGameSimulationInitialize()
		{
			ShowView(instant: true);
		}
		#endregion
		
		#region CursorModel Events
		void OnCursorCurrent(CursorModel.Selection current)
		{
			switch (current.State)
			{
				case CursorModel.States.Highlighting:
					View.Highlight(current.Begin, current.End);
					foreach (var clearable in game.Clearables)
					{
						clearable.SelectionState.Value = current.Contains(clearable.Position.Value) ? SelectionStates.Highlighted : SelectionStates.Deselected;
					}
					break;
				case CursorModel.States.Selected:
					View.Select(current.Begin, current.End);
					foreach (var clearable in game.Clearables)
					{
						clearable.SelectionState.Value = current.Contains(clearable.Position.Value) ? SelectionStates.Selected : SelectionStates.Deselected;
					}
					break;
				default:
					View.None();
					break;
			}
		}
		#endregion
		
		#region Utility
		Ray CurrentRay => game.WorldCamera.CameraInstance.Value.ScreenPointToRay(UnityInput.mousePosition);
		bool HasCollision(out RaycastHit hit) => Physics.Raycast(CurrentRay, out hit, float.MaxValue, LayerMasks.Floor);
		#endregion
	}
}