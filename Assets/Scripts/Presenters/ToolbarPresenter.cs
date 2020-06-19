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
			game.Interaction.Scroll.Changed += OnInteractionScroll;

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
			game.Interaction.Scroll.Changed -= OnInteractionScroll;
			
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
			View.ConstructWallClick += OnConstructWallClick;
			
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
		void OnInteractionFloorSelection(Interaction.RoomVector3 interaction)
		{
			switch (toolbar.Task.Value)
			{
				case ToolbarModel.Tasks.Clearance:
					toolbar.ClearanceTask.Value = interaction;
					break;
				case ToolbarModel.Tasks.Construction:
					toolbar.ConstructionTranslation.Value = interaction;
					break;
				case ToolbarModel.Tasks.None: break;
				default:
					Debug.LogError("Unrecognized Task: "+toolbar.Task.Value);
					break;
			}
		}

		void OnInteractionScroll(Interaction.GenericVector3 interaction)
		{
			switch (toolbar.Task.Value)
			{
				case ToolbarModel.Tasks.Clearance:
					break;
				case ToolbarModel.Tasks.Construction:
					toolbar.ConstructionRotation.Value = new Interaction.GenericFloat(
						interaction.State,
						new Interaction.DeltaFloat(
							interaction.Value.Begin.y * 4f, // TODO: Don't hardcode these sensitivity values..
							interaction.Value.End.y * 4f
						)
					);
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
		
		void OnConstructWallClick()
		{
			View.ConstructWallSelected = ToggleTask(
				ToolbarModel.Tasks.Construction,
				Buildings.WallSmall
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
			View.ConstructWallSelected = false;
		}
		#endregion
	}
}