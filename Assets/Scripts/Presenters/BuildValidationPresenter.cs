using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;

namespace Lunra.Hothouse.Presenters
{
	public class BuildValidationPresenter : Presenter<BuildValidationView>
	{
		GameModel game;
		BuildValidationModel buildValidation;

		ulong lastUpdateCount;
		LightingResult lastLightValueCalculated;

		public BuildValidationPresenter(GameModel game)
		{
			this.game = game;
			buildValidation = game.BuildValidation; 

			game.Toolbar.Task.Changed += OnToolbarTask;
			game.Toolbar.IsEnabled.Changed += OnToolbarIsEnabled;
			game.Toolbar.ConstructionTranslation.Changed += OnToolbarConstructionTranslation;
			game.Toolbar.ConstructionRotation.Changed += OnToolbarConstructionRotation;

			buildValidation.Current.Changed += OnBuildValidationCurrent;
		}

		protected override void UnBind()
		{
			game.Toolbar.Task.Changed -= OnToolbarTask;
			game.Toolbar.IsEnabled.Changed -= OnToolbarIsEnabled;
			game.Toolbar.ConstructionTranslation.Changed -= OnToolbarConstructionTranslation;
			game.Toolbar.ConstructionRotation.Changed -= OnToolbarConstructionRotation;
			
			buildValidation.Current.Changed -= OnBuildValidationCurrent;
		}

		void Show()
		{
			if (View.Visible) return;
			
			View.Reset();

			ShowView(instant: true);
		}

		void Close()
		{
			if (View.NotVisible) return;
			
			CloseView(true);
		}

		void UpdateValidation(Interaction.RoomVector3 interaction)
		{
			if (lastUpdateCount == App.Heartbeat.UpdateCount) return;

			lastUpdateCount = App.Heartbeat.UpdateCount;
			
			var navMeshHitSuccess = NavMesh.SamplePosition(
				game.Toolbar.Building.Value.Transform.Position.Value,
				out _,
				0.25f, // TODO: Don't hardcode this value...
				NavMesh.AllAreas
			);

			if (!navMeshHitSuccess)
			{
				buildValidation.Current.Value = BuildValidationModel.Validation.Invalid(
					interaction,
					"Cannot build here"
				);
				return;
			}

			game.Toolbar.Building.Value.RoomTransform.Id.Value = interaction.RoomId;
			
			lastLightValueCalculated = game.CalculateMaximumLighting(
				(
					interaction.RoomId,
					interaction.Value.Begin,
					new ILightModel[] { game.Toolbar.Building.Value }
				)
			);
			var placementLightRequirement = game.Toolbar.Building.Value.PlacementLightRequirement.Value;
			
			if (lastLightValueCalculated.OperatingMaximum < placementLightRequirement.Minimum)
			{
				buildValidation.Current.Value = BuildValidationModel.Validation.Invalid(
					interaction,
					"Too far from an existing light source"
				);
				return;
			}
			
			if (placementLightRequirement.Maximum < lastLightValueCalculated.OperatingMaximum)
			{
				buildValidation.Current.Value = BuildValidationModel.Validation.Invalid(
					interaction,
					"Too close to an existing light source"
				);
				return;
			}

			if (placementLightRequirement.Maximum < lastLightValueCalculated.ConstructingMaximum)
			{
				buildValidation.Current.Value = BuildValidationModel.Validation.Invalid(
					interaction,
					"Too close to a light source under construction"
				);
				return;
			}

			if (game.Toolbar.Building.Value.Enterable.Entrances.Value.None(e => e.IsNavigable))
			{
				buildValidation.Current.Value = BuildValidationModel.Validation.Invalid(
					interaction,
					"Entrances blocked"
				);
				return;
			}

			var room = game.Rooms.FirstActive(m => m.Id.Value == game.Toolbar.Building.Value.RoomTransform.Id.Value);
			
			bool IsFloraColliding(FloraModel flora)
			{
				// if (!game.Toolbar.Building.Value.BoundaryContains(flora.Transform.Position.Value)) return false;
				if (room.Id.Value != flora.RoomTransform.Id.Value && !room.AdjacentRoomIds.Value.ContainsKey(flora.RoomTransform.Id.Value)) return false;
				return game.Toolbar.Building.Value.Boundary.Contains(flora.Transform.Position.Value);
			}
			
			if (game.Flora.AllActive.Any(IsFloraColliding))
			{
				buildValidation.Current.Value = BuildValidationModel.Validation.Invalid(
					interaction,
					"Clear flora before placing"
				);
				return;
			}

			buildValidation.Current.Value = BuildValidationModel.Validation.Valid(interaction);
		}

		#region ToolbarModel Events
		void OnToolbarTask(ToolbarModel.Tasks task)
		{
			if (task == ToolbarModel.Tasks.Construction) Show();
			else Close();
		}

		void OnToolbarIsEnabled(bool isEnabled)
		{
			OnToolbarTask(game.Toolbar.Task.Value);
		}
		
		void OnToolbarConstructionTranslation(Interaction.RoomVector3 interaction)
		{
			switch (interaction.State)
			{
				case Interaction.States.OutOfRange:
				case Interaction.States.Cancel:
					Close();
					if (buildValidation.Current.Value.State != BuildValidationModel.ValidationStates.None)
					{
						buildValidation.Current.Value = BuildValidationModel.Validation.None();
					}
					return;
				case Interaction.States.End:
					UpdateValidation(interaction);
					Close();
					switch (buildValidation.Current.Value.State)
					{
						case BuildValidationModel.ValidationStates.None:
							Debug.LogWarning("It should not be possible for the validation state to be: "+buildValidation.Current.Value.State);
							break;
						case BuildValidationModel.ValidationStates.Invalid:
							break;
						case BuildValidationModel.ValidationStates.Valid:
							game.Toolbar.Building.Value.BuildingState.Value = BuildingStates.Constructing;
							game.Toolbar.Task.Value = ToolbarModel.Tasks.None;
							game.Toolbar.Building.Value = null;
							break;
						default:
							Debug.LogError("Unrecognized Validation: "+buildValidation.Current.Value.State);
							break;
					}
					return;
				case Interaction.States.Idle:
				case Interaction.States.Begin:
				case Interaction.States.Active:
					Show();
					break;
				default:
					Debug.LogError("Unrecognized State: "+interaction.State);
					return;
			}
			
			UpdateValidation(interaction);
		}

		void OnToolbarConstructionRotation(Interaction.GenericFloat interaction)
		{
			if (View.Visible) UpdateValidation(game.Toolbar.ConstructionTranslation.Value);
		}
		#endregion
		
		#region BuildValidationModel Events
		void OnBuildValidationCurrent(BuildValidationModel.Validation current)
		{
			if (View.NotVisible) return;

			switch (current.Interaction.State)
			{
				case Interaction.States.Idle:
				case Interaction.States.Begin:
					break;
				case Interaction.States.OutOfRange:
				case Interaction.States.Active:
				case Interaction.States.End:
				case Interaction.States.Cancel:
					return;
				default:
					Debug.LogError("Unrecognized interaction state: "+current.Interaction.State);
					return;
			}
			
			View.RootTransform.position = current.Interaction.Value.Begin;
			View.CameraForward = game.WorldCamera.CameraInstance.Value.transform.forward;
			
			View.UpdateValidation(
				current.State,
				current.Message
			);

			View.LightLevel = lastLightValueCalculated.OperatingMaximum;
		}
		#endregion
	}
}