using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class BuildingManagePresenter : Presenter<BuildingManageView>
	{
		GameModel game;

		BuildingStates lastBuildingState;

		public BuildingManagePresenter(GameModel game)
		{
			this.game = game;
			
			game.SimulationUpdate += OnGameSimulationUpdate;
			
			game.Toolbar.IsEnabled.Changed += OnToolbarIsEnabled;

			game.BuildingManage.Selection.Changed += OnBuildingManageSelection;
			
			Show();
		}

		protected override void UnBind()
		{
			game.SimulationUpdate -= OnGameSimulationUpdate;
			
			game.Toolbar.IsEnabled.Changed -= OnToolbarIsEnabled;
			
			game.BuildingManage.Selection.Changed -= OnBuildingManageSelection;
		}

		void Show()
		{
			if (View.Visible) return;
			
			View.Cleanup();

			View.Prepare += () => OnBuildingManageSelection(game.BuildingManage.Selection.Value);

			ShowView(instant: true);
		}
		
		#region GameModelEvents
		void OnGameSimulationUpdate()
		{
			if (game.BuildingManage.Selection.Value == null || lastBuildingState == game.BuildingManage.Selection.Value.BuildingState.Value) return;
			
			OnBuildingManageSelection(game.BuildingManage.Selection.Value);
		}
		#endregion
		
		#region BuildingManageModel Events
		void OnBuildingManageSelection(BuildingModel selection)
		{
			if (selection == null)
			{
				View.Controls();
				return;
			}

			lastBuildingState = selection.BuildingState.Value;

			switch (selection.BuildingState.Value)
			{
				case BuildingStates.Constructing:
					OnBuildingManageSelectionConstructing(selection);
					break;
				case BuildingStates.Operating:
					OnBuildingManageSelectionOperating(selection);
					break;
				case BuildingStates.Salvaging:
					OnBuildingManageSelectionSalvaging(selection);
					break;
				case BuildingStates.Placing:
					View.Controls();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		void OnBuildingManageSelectionConstructing(BuildingModel selection)
		{
			View.Controls(
				new BuildingManageView.Control
				{
					Type = BuildingManageView.Control.Types.Label,
					LabelText = "Construction: TODO"
				}
			);
		}
		
		void OnBuildingManageSelectionOperating(BuildingModel selection)
		{
			var controls = new List<BuildingManageView.Control>();
			
			controls.Add(
				new BuildingManageView.Control
				{
					Type = BuildingManageView.Control.Types.Label,
					LabelText = $"[ {selection.ShortId} ] {selection.Type.Value} - "+selection.BuildingState.Value
				}
			);
			
			controls.Add(
				new BuildingManageView.Control
				{
					Type = BuildingManageView.Control.Types.Label,
					LabelText = $"\n Health: {selection.Health.Current.Value:N0} / {selection.Health.Maximum.Value:N0}"
				}
			);
			
			
			
			View.Controls(controls.ToArray());
		}
		
		void OnBuildingManageSelectionSalvaging(BuildingModel selection)
		{
			View.Controls(
				new BuildingManageView.Control
				{
					Type = BuildingManageView.Control.Types.Label,
					LabelText = "Salvage: TODO"
				}
			);
		}
		#endregion
		
		#region ToolbarModel Events
		void OnToolbarIsEnabled(bool isEnabled)
		{
			if (isEnabled) Show();
			else if (View.Visible) CloseView(true);
		}
		#endregion
	}
}