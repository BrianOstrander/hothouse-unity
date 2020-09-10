using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Presenters;
using UnityEngine;
using UnityEngine.Assertions;

namespace Lunra.Hothouse.Presenters
{
	public class ToolbarPresenter : Presenter<ToolbarView>
	{
		static string GetGenericTaskId(ToolbarModel.Tasks task) => task.ToString();
		static string GetConstructionTaskId(string buildingType) => GetGenericTaskId(ToolbarModel.Tasks.Construction) + "_" + buildingType;

		Dictionary<string, string> taskLabels;
		Dictionary<string, string> TaskLabels
		{
			get
			{
				if (taskLabels != null) return taskLabels;
				
				taskLabels = new Dictionary<string, string>();

				taskLabels.Add(
					GetGenericTaskId(ToolbarModel.Tasks.Cancel),
					"Cancel"
				);
				
				taskLabels.Add(
					GetGenericTaskId(ToolbarModel.Tasks.Clearance),
					"Gather"
				);
				
				foreach (var building in game.Buildings.Definitions)
				{
					taskLabels.Add(
						GetConstructionTaskId(building.Type),
						"Build " + building.Type
					);
				}

				return taskLabels;
			}
		}

		Dictionary<string, Action> taskActions;
		Dictionary<string, Action> TaskActions
		{
			get
			{
				if (taskActions != null) return taskActions;
				
				taskActions = new Dictionary<string, Action>();
				
				foreach (var task in EnumExtensions.GetValues(ToolbarModel.Tasks.Unknown, ToolbarModel.Tasks.None))
				{
					switch (task)
					{
						case ToolbarModel.Tasks.Cancel:
							taskActions.Add(
								GetGenericTaskId(task),
								() => OnGenericSelectionClick(task)
							);
							break;
						case ToolbarModel.Tasks.Clearance:
							taskActions.Add(
								GetGenericTaskId(task),
								() => OnGenericSelectionClick(task)
							);
							break;
						case ToolbarModel.Tasks.Construction:
							foreach (var building in game.Buildings.Definitions)
							{
								taskActions.Add(
									GetConstructionTaskId(building.Type),
									() => OnConstructSelectionClick(building)
								);
							}
							break;
						default:
							Debug.LogError("Unrecognized task: " + task);
							break;
					}
				}

				return taskActions;
			}
		}

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
				toolbar.CancelTask
			);
			
			new RadialCursorPresenter(
				game,
				toolbar.ClearanceTask
			);
		}

		protected override void Deconstruct()
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
			
			View.Cleanup();
			
			View.Selection += OnViewSelection;
			
			View.InitializeControls(
				TaskLabels.Select(
					kv => new ToolbarView.Control
					{
						Id = kv.Key,
						Label = kv.Value
					}
				)
				.ToArray()
			);

			View.SetSelection();
			
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
				case ToolbarModel.Tasks.Cancel:
					toolbar.CancelTask.Value = interaction;
					break;
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
				case ToolbarModel.Tasks.Cancel:
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
		void OnViewSelection(string id)
		{
			if (!TaskActions.TryGetValue(id, out var callback))
			{
				Debug.LogError("No callback registered for id: " + id);
				return;
			}

			if (callback == null)
			{
				Debug.LogError("Callback with id \"" + id + "\" is null");
				return;
			}
			
			callback();
		}
		
		void OnGenericSelectionClick(ToolbarModel.Tasks task)
		{
			if (ToggleTask(task)) View.SetSelection(GetGenericTaskId(task));
		}
		
		void OnConstructSelectionClick(BuildingDefinition buildingDefinition)
		{
			if (ToggleTask(ToolbarModel.Tasks.Construction, buildingDefinition)) View.SetSelection(GetConstructionTaskId(buildingDefinition.Type));
		}
		#endregion
		
		#region Utility
		bool ToggleTask(
			ToolbarModel.Tasks task,
			BuildingDefinition buildingDefinition = null
		)
		{
			Assert.IsFalse(task == ToolbarModel.Tasks.None, "It should not be possible to toggle to "+nameof(ToolbarModel.Tasks.None));

			switch (task)
			{
				case ToolbarModel.Tasks.Cancel:
				case ToolbarModel.Tasks.Clearance:
					task = task == toolbar.Task.Value ? ToolbarModel.Tasks.None : task;
					break;
				case ToolbarModel.Tasks.Construction:
					task = toolbar.Building.Value != null && toolbar.Building.Value.Type.Value == buildingDefinition.Type ? ToolbarModel.Tasks.None : task;
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
					buildingDefinition,
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
			
			View.SetSelection();
		}
		#endregion
	}
}