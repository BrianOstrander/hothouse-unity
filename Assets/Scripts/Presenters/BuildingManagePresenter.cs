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

		DateTime lastBuildingUpdate;
		BuildingStates lastBuildingState;

		public BuildingManagePresenter(GameModel game)
		{
			this.game = game;
			
			game.SimulationUpdate += OnGameSimulationUpdate;
			
			game.Toolbar.IsEnabled.Changed += OnToolbarIsEnabled;

			game.BuildingManage.Selection.Changed += OnBuildingManageSelection;

			game.Interaction.FloorSelection.Changed += OnInteractionFloorSelection;
			
			Show();
		}

		protected override void UnBind()
		{
			game.SimulationUpdate -= OnGameSimulationUpdate;
			
			game.Toolbar.IsEnabled.Changed -= OnToolbarIsEnabled;
			
			game.BuildingManage.Selection.Changed -= OnBuildingManageSelection;
			
			game.Interaction.FloorSelection.Changed -= OnInteractionFloorSelection;
		}

		void Show()
		{
			if (View.Visible) return;
			
			View.Cleanup();

			View.RefreshControls += () => OnBuildingManageSelection(game.BuildingManage.Selection.Value);

			View.Prepare += () => OnBuildingManageSelection(game.BuildingManage.Selection.Value);

			ShowView(instant: true);
		}
		
		#region GameModelEvents
		void OnGameSimulationUpdate()
		{
			if (game.BuildingManage.Selection.Value == null) return;
			if (lastBuildingState == game.BuildingManage.Selection.Value.BuildingState.Value && (DateTime.Now - lastBuildingUpdate).TotalSeconds < 1.0) return;
			
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
			lastBuildingUpdate = DateTime.Now;

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
			var controls = new List<BuildingManageView.Control>();
			
			if (selection.Obligations.HasAny(ObligationCategories.Construct.Assemble))
			{
				var obligationResult = "<b>Obligations</b>";
				foreach (var obligation in selection.Obligations.All.Value.Available)
				{
					obligationResult += $"\n - {obligation.Type}";
				}
				
				foreach (var obligation in selection.Obligations.All.Value.Forbidden)
				{
					obligationResult += $"\n - {obligation.Type} : [ QUEUED ]";
				}
				
				controls.Add(
					new BuildingManageView.Control(
						BuildingManageView.Control.Types.Label,
						obligationResult.WrapInArray()
					)
				);
			}
			
			var inventoryResult = string.Empty;
			foreach (var inventoryType in selection.ConstructionInventory.AllCapacity.Value.GetMaximum().Entries.Where(e => 0 < e.Weight).Select(e => e.Type))
			{
				inventoryResult += $"\n\t {inventoryType}: \t{selection.ConstructionInventory.All.Value[inventoryType]} \t/ {selection.ConstructionInventory.AllCapacity.Value.GetMaximumFor(inventoryType)}";
			}

			if (!string.IsNullOrEmpty(inventoryResult))
			{
				inventoryResult = "<b>Construction</b>" + inventoryResult;
				controls.Add(
					new BuildingManageView.Control(
						BuildingManageView.Control.Types.Label,
						inventoryResult.WrapInArray()
					)
				);
			}
			
			View.Controls(controls.ToArray());
		}
		
		void OnBuildingManageSelectionOperating(BuildingModel selection)
		{
			var controls = new List<BuildingManageView.Control>();
			
			controls.Add(
				new BuildingManageView.Control(
					BuildingManageView.Control.Types.Label,
					($"[ {selection.ShortId} ] {selection.Type.Value} - "+selection.BuildingState.Value).WrapInArray()
				)
			);
			
			controls.Add(
				new BuildingManageView.Control(
					BuildingManageView.Control.Types.Label,
					$"\n Health: {selection.Health.Current.Value:N0} / {selection.Health.Maximum.Value:N0}".WrapInArray()
				)
			);
			
			if (0 < selection.Ownership.MaximumClaimers.Value)
			{
				var ownerResult = $"<b>Owners</b> {selection.Ownership.Claimers.Value.Length} / {selection.Ownership.PermittedClaimers.Value} / {selection.Ownership.MaximumClaimers.Value}";
				
				controls.Add(
					new BuildingManageView.Control(
						BuildingManageView.Control.Types.LeftRightButton,
						ownerResult.WrapInArray(),
						new [] { "<", ">" },
						new Action[]
						{
							() =>
							{
								if (0 < selection.Ownership.PermittedClaimers.Value)
								{
									selection.Ownership.PermittedClaimers.Value--;
									if (selection.Ownership.PermittedClaimers.Value < selection.Ownership.Claimers.Value.Length)
									{
										selection.Ownership.Remove(selection.Ownership.Claimers.Value.First());
									}
								}
							},
							() =>
							{
								if (selection.Ownership.PermittedClaimers.Value < selection.Ownership.MaximumClaimers.Value)
								{
									selection.Ownership.PermittedClaimers.Value++;
								}
							}
						}
					)
				);

				var ownerListResult = string.Empty;
				for (var i = 0; i < selection.Ownership.PermittedClaimers.Value; i++)
				{
					ownerListResult += $"\n - [ {i} ] ";
					if (i < selection.Ownership.Claimers.Value.Length)
					{
						if (selection.Ownership.Claimers.Value[i].TryGetInstance<DwellerModel>(game, out var owner))
						{
							ownerListResult += owner.Name.Value;
						}
						else ownerListResult += "< MISSING >";
					}
					else ownerListResult += "NONE";
				}
				
				controls.Add(
					new BuildingManageView.Control(
						BuildingManageView.Control.Types.Label,
						ownerListResult.WrapInArray()
					)
				);
			}
			
			if (selection.Obligations.HasAny(ObligationCategories.Construct.Assemble))
			{
				var obligationResult = "<b>Obligations</b>";
				foreach (var obligation in selection.Obligations.All.Value.Available)
				{
					obligationResult += $"\n - {obligation.Type}";
				}
				
				foreach (var obligation in selection.Obligations.All.Value.Forbidden)
				{
					obligationResult += $"\n - {obligation.Type} : [ QUEUED ]";
				}
				
				controls.Add(
					new BuildingManageView.Control(
						BuildingManageView.Control.Types.Label,
						obligationResult.WrapInArray()
					)
				);
			}

			var inventoryResult = string.Empty;
			foreach (var inventoryType in selection.Inventory.AllCapacity.Value.GetMaximum().Entries.Where(e => 0 < e.Weight).Select(e => e.Type))
			{
				inventoryResult += $"\n\t {inventoryType}: \t{selection.Inventory.All.Value[inventoryType]} \t/ {selection.Inventory.AllCapacity.Value.GetMaximumFor(inventoryType)}";
			}

			if (!string.IsNullOrEmpty(inventoryResult))
			{
				inventoryResult = "<b>Inventory</b>" + inventoryResult;
				controls.Add(
					new BuildingManageView.Control(
						BuildingManageView.Control.Types.Label,
						inventoryResult.WrapInArray()
					)
				);
			}

			if (selection.Recipes.Available.Value.Any())
			{
				foreach (var recipe in selection.Recipes.Available.Value)
				{
					var isInQueue = selection.Recipes.Queue.Value.Any(r => r.Recipe.Id == recipe.Id);
					
					controls.Add(
						new BuildingManageView.Control(
							isInQueue ? BuildingManageView.Control.Types.RadioButtonEnabled : BuildingManageView.Control.Types.RadioButtonDisabled,
							$"{recipe.Name} [ {(isInQueue ? "ENABLED" : "DISABLED")} ]".WrapInArray(),
							click: new Action[]
							{
								() =>
								{
									if (isInQueue) selection.Recipes.Queue.Value = new RecipeComponent.RecipeIteration[0];
									else selection.Recipes.Queue.Value = new [] { RecipeComponent.RecipeIteration.ForInfinity(recipe) };
								}
							}
						)
					);
				}	
			}
			
			View.Controls(controls.ToArray());
		}
		
		void OnBuildingManageSelectionSalvaging(BuildingModel selection)
		{
			View.Controls(
				new BuildingManageView.Control(
					BuildingManageView.Control.Types.Label,
					"Salvage: TODO".WrapInArray()
				)
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
		
		#region GameInteractionModel Events
		void OnInteractionFloorSelection(Interaction.RoomVector3 selection)
		{
			if (selection.State != Interaction.States.End) return;
			if (string.IsNullOrEmpty(selection.RoomId)) return;
			if (0.1f < selection.Value.Delta.magnitude) return;

			var nearestBuilding = game.Buildings.AllActive
				.Where(m => m.IsInRoom(selection.RoomId))
				.OrderBy(m => Vector3.Distance(m.Transform.Position.Value, selection.Value.End))
				.FirstOrDefault();

			if (nearestBuilding == null || !nearestBuilding.Boundary.Contains(selection.Value.End))
			{
				game.BuildingManage.Selection.Value = null;
				return;
			}

			game.BuildingManage.Selection.Value = nearestBuilding;
		}
		#endregion
	}
}