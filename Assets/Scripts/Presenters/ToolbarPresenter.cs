using System;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Presenters;
using UnityEngine;
using UnityEngine.Assertions;

namespace Lunra.Hothouse.Presenters
{
	public class ToolbarPresenter : Presenter<ToolbarView>
	{
		GameModel game;
		ToolbarModel toolbar;
		
		public ToolbarPresenter(
			GameModel game
		)
		{
			this.game = game;
			toolbar = game.Toolbar;
			
			game.SimulationInitialize += OnGameSimulationInitialize;

			game.Interaction.FloorSelection.Changed += OnInteractionFloorSelection;

			toolbar.IsEnabled.Changed += OnToolbarIsEnabled;
			toolbar.Task.Changed += OnToolbarTask;
			
			new RadialCursorPresenter(
				game,
				toolbar.ClearanceTask
			);
		}

		protected override void UnBind()
		{
			game.SimulationInitialize -= OnGameSimulationInitialize;
			
			game.Interaction.FloorSelection.Changed -= OnInteractionFloorSelection;
			
			toolbar.IsEnabled.Changed -= OnToolbarIsEnabled;
			toolbar.Task.Changed -= OnToolbarTask;
		}
		
		void Show()
		{
			if (View.Visible) return;
			
			View.Reset();

			View.ClearanceClick += OnClearanceClick;
			View.ConstructFireClick += OnConstructFireClick;
			View.ConstructBedClick += OnConstructBedClick;
			
			ShowView(instant: true);
		}

		void Close()
		{
			if (View.NotVisible) return;
			
			CloseView(true);
		}
		
		#region GameModel Events
		void OnGameSimulationInitialize()
		{
			OnToolbarIsEnabled(toolbar.IsEnabled.Value);
		}
		#endregion
		
		#region ToolbarModel Events
		void OnToolbarIsEnabled(bool isEnabled)
		{
			if (isEnabled) Show();
			else Close();
		}

		void OnToolbarTask(ToolbarModel.Tasks task)
		{
			ResetSelections();
		}
		#endregion
		
		#region InteractionModel Events
		void OnInteractionFloorSelection(Interaction.Generic interaction)
		{
			switch (toolbar.Task.Value)
			{
				case ToolbarModel.Tasks.Clearance:
					toolbar.ClearanceTask.Value = interaction;
					break;
				case ToolbarModel.Tasks.Construction:
					toolbar.ConstructionTask.Value = interaction;
					break;
				case ToolbarModel.Tasks.None: break;
				default:
					Debug.LogError("Unrecognized Task: "+toolbar.Task.Value);
					break;
			}
		}
		#endregion
		
		#region View Events
		void OnClearanceClick()
		{
			View.ClearanceSelected = ToggleTask(ToolbarModel.Tasks.Clearance);
		}

		void OnConstructFireClick()
		{
			View.ConstructFireSelected = ToggleTask(
				ToolbarModel.Tasks.Construction,
				Buildings.Bonfire
			);
			
		}
		
		void OnConstructBedClick()
		{
			View.ConstructBedSelected = ToggleTask(
				ToolbarModel.Tasks.Construction,
				Buildings.Bedroll
			);
		}
		#endregion
		
		#region Utility
		bool ToggleTask(
			ToolbarModel.Tasks task,
			Buildings building = Buildings.Unknown
		)
		{
			Assert.IsFalse(task == ToolbarModel.Tasks.None, "It should not be possible to toggle to "+nameof(ToolbarModel.Tasks.None));

			switch (task)
			{
				case ToolbarModel.Tasks.Clearance:
					task = task == toolbar.Task.Value ? ToolbarModel.Tasks.None : task;
					break;
				case ToolbarModel.Tasks.Construction:
					task = toolbar.Building.Value != null && toolbar.Building.Value.Type.Value == building ? ToolbarModel.Tasks.None : task;
					break;
				default:
					Debug.LogError("Unrecognized Task: " + task);
					break;
			}
			
			if (toolbar.Building.Value != null)
			{
				toolbar.Building.Value.PooledState.Value = PooledStates.InActive;
				toolbar.Building.Value = null;
			}

			if (task == ToolbarModel.Tasks.Construction)
			{
				toolbar.Building.Value = game.Buildings.Activate(
					building,
					game.Rooms.FirstActive().Id.Value,
					Vector3.down * 100f, // Just stick it under everything for the moment...
					Quaternion.identity,
					BuildingStates.Placing
				);	
			}

			toolbar.Task.Value = task;
			
			ResetSelections();
			
			return task != ToolbarModel.Tasks.None;
		}

		void ResetSelections()
		{
			if (View.NotVisible) return;
			
			View.ClearanceSelected = false;
			View.ConstructFireSelected = false;
			View.ConstructBedSelected = false;
		}
		#endregion
	}
}