using System.Linq;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;
using UnityEngine;

namespace Lunra.WildVacuum.Presenters
{
	public class SelectionPresenter : Presenter<SelectionView>
	{
		GameModel game;
		SelectionModel selection;

		public SelectionPresenter(
			GameModel game
		)
		{
			this.game = game;
			selection = game.Selection;

			game.SimulationInitialize += OnGameSimulationInitialize;
			
			selection.Current.Changed += OnSelectionCurrent;

			App.Heartbeat.Update += OnHeartbeatUpdate;
		}

		protected override void OnUnBind()
		{
			game.SimulationInitialize -= OnGameSimulationInitialize;
			
			selection.Current.Changed -= OnSelectionCurrent;
			
			App.Heartbeat.Update -= OnHeartbeatUpdate;
		}
		
		#region Heartbeat Events
		void OnHeartbeatUpdate()
		{
			if (Input.GetMouseButtonDown(0)) OnInputMousePrimaryDown();
			else if (Input.GetMouseButton(0)) OnInputMousePrimaryPressed();
			else if (Input.GetMouseButtonUp(0)) OnInputMousePrimaryUp();
			
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
			
			selection.Current.Value = SelectionModel.Selection.Highlighting(hit.point, hit.point);
		}
		
		void OnInputMousePrimaryPressed()
		{
			if (selection.Current.Value.State != SelectionModel.States.Highlighting) return;

			var currentRay = CurrentRay;
			if (!selection.Current.Value.Surface.Raycast(currentRay, out var surfaceDistance)) return;
			
			selection.Current.Value = SelectionModel.Selection.Highlighting(
				selection.Current.Value.Begin,
				currentRay.origin + (currentRay.direction * surfaceDistance)
			);
		}
		
		void OnInputMousePrimaryUp()
		{
			if (selection.Current.Value.State != SelectionModel.States.Highlighting) return;

			selection.Current.Value = selection.Current.Value.NewState(SelectionModel.States.Selected);
		}
		#endregion
		
		#region GameModel Events
		void OnGameSimulationInitialize()
		{
			ShowView(instant: true);
		}
		#endregion
		
		#region SelectionModel Events
		void OnSelectionCurrent(SelectionModel.Selection current)
		{
			switch (current.State)
			{
				case SelectionModel.States.Highlighting:
					View.Highlight(current.Begin, current.End);
					foreach (var flora in game.Flora.AllActive)
					{
						flora.SelectionState.Value = current.Contains(flora.Position.Value) ? SelectionStates.Highlighted : SelectionStates.Deselected;
					}
					break;
				case SelectionModel.States.Selected:
					View.Select(current.Begin, current.End);
					foreach (var flora in game.Flora.AllActive)
					{
						flora.SelectionState.Value = current.Contains(flora.Position.Value) ? SelectionStates.Selected : SelectionStates.Deselected;
					}
					break;
				default:
					View.None();
					break;
			}
		}
		#endregion
		
		#region Utility
		Ray CurrentRay => game.WorldCamera.CameraInstance.Value.ScreenPointToRay(Input.mousePosition);
		bool HasCollision(out RaycastHit hit) => Physics.Raycast(CurrentRay, out hit);
		#endregion
	}
}