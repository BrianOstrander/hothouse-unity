using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public abstract class BaseBuildingPresenter<M, V> : PrefabPresenter<M, V>
		where M : BuildingModel
		where V : BuildingView
	{
		protected BaseBuildingPresenter(GameModel game, M model) : base(game, model) { }

		protected override void Bind()
		{
			if (View.Entrances.None())
			{
				Debug.LogError(
					"No entrances found for "+View.name+"."+StringExtensions.GetNonNullOrEmpty(View.PrefabId, "< null or empty PrefabId >")
				);
			}

			Model.RadialBoundary.Radius.Value = View.NavigationColliderRadius;
			Model.Light.IsLight.Value = View.IsLight;
			
			if (Model.Light.IsLight.Value)
			{
				Model.Light.LightRange.Value = View.LightRange;
				Model.Light.ReseltLightCalculationsEnabled(Model.IsBuildingState(BuildingStates.Operating));

				// ILightModel Bindings
				Game.SimulationUpdate += OnLightSimulationUpdate;
				Model.Light.LightState.Changed += OnLightState;
				Model.Inventory.Changed += OnLightBuildingInventory;
				Model.BuildingState.Changed += OnLightBuildingState;
			}

			// Misc Bindings
			Game.Toolbar.ConstructionTask.Changed += OnToolbarConstruction;
			Game.Toolbar.Task.Changed += OnToolbarTask;
			Game.NavigationMesh.CalculationState.Changed += OnNavigationMeshCalculationState;

			Model.Inventory.Changed += OnBuildingInventory;
			Model.ConstructionInventory.Changed += OnBuildingConstructionInventory;
			Model.SalvageInventory.Changed += OnBuildingSalvageInventory;
			Model.BuildingState.Changed += OnBuildingState;
			Model.LightSensitive.LightLevel.Changed += OnBuildingLightLevel;
			Model.Operate += OnBuildingOperate;

			base.Bind();
		}

		protected override void UnBind()
		{
			// ILightModel UnBindings
			Game.SimulationUpdate -= OnLightSimulationUpdate;
			Model.Light.LightState.Changed -= OnLightState;
			Model.Inventory.Changed -= OnLightBuildingInventory;
			Model.BuildingState.Changed -= OnLightBuildingState;
			
			// Misc UnBindings
			Game.Toolbar.ConstructionTask.Changed -= OnToolbarConstruction;
			Game.Toolbar.Task.Changed -= OnToolbarTask;
			Game.NavigationMesh.CalculationState.Changed -= OnNavigationMeshCalculationState;
			
			Model.Inventory.Changed -= OnBuildingInventory;
			Model.ConstructionInventory.Changed -= OnBuildingConstructionInventory;
			Model.SalvageInventory.Changed -= OnBuildingSalvageInventory;
			Model.BuildingState.Changed -= OnBuildingState;
			Model.LightSensitive.LightLevel.Changed -= OnBuildingLightLevel;
			Model.Operate -= OnBuildingOperate;
			
			base.UnBind();
		}

		protected override void OnSimulationInitialized()
		{
			OnBuildingInventory(Model.Inventory.Value);
		}
		
		#region LightSourceModel Events
		protected virtual void OnLightSimulationUpdate()
		{
			if (IsNotActive) return;
			if (Model.BuildingState.Value != BuildingStates.Operating) return;
			if (Model.Light.LightState.Value == LightStates.Extinguished) return;

			Model.Light.LightFuelInterval.Value = Model.Light.LightFuelInterval.Value.Update(Game.SimulationDelta);

			if (Model.Light.LightFuelInterval.Value.IsDone)
			{
				var canRefuel = Model.Inventory.Value.Contains(Model.Light.LightFuel.Value);

				if (canRefuel)
				{
					Model.Inventory.Value -= Model.Light.LightFuel.Value;
					Model.Light.LightFuelInterval.Value = Model.Light.LightFuelInterval.Value.Restarted();
					if (View.Visible) View.LightFuelNormal = 1f;
					return;
				}

				switch (Model.Light.LightState.Value)
				{
					case LightStates.Extinguishing:
						if (View.Visible) View.LightFuelNormal = 0f;
						Model.Light.LightState.Value = LightStates.Extinguished;
						return;
					case LightStates.Fueled:
						Model.Light.LightState.Value = LightStates.Extinguishing;
						Model.Light.LightFuelInterval.Value = Model.Light.LightFuelInterval.Value.Restarted();
						return;
					default:
						Debug.LogError("Unrecognized LightState: "+Model.Light.LightState.Value);
						return;
				}
			}

			switch (Model.Light.LightState.Value)
			{
				case LightStates.Extinguishing:
					if (View.Visible) View.LightFuelNormal = Model.Light.LightFuelInterval.Value.InverseNormalized;
					break;
			}
		}
		
		protected virtual void OnLightState(LightStates lightState)
		{
			if (IsNotActive) return;
			Game.LastLightUpdate.Value = Game.LastLightUpdate.Value.SetRoomStale(Model.RoomTransform.Id.Value);
			
			switch (lightState)
			{
				case LightStates.Extinguished:
					Model.BuildingState.Value = BuildingStates.Salvaging;
					break;
			}
		}

		protected virtual void OnLightBuildingInventory(Inventory inventory)
		{
			if (IsNotActive) return;
			if (Model.Light.LightState.Value != LightStates.Extinguishing) return;
			if (!Model.Inventory.Value.Contains(Model.Light.LightFuel.Value)) return;
			
			Model.Inventory.Value -= Model.Light.LightFuel.Value;
			Model.Light.LightFuelInterval.Value = Model.Light.LightFuelInterval.Value.Restarted();
			if (View.Visible) View.LightFuelNormal = 1f;
			Model.Light.LightState.Value = LightStates.Fueled;
		}
		
		void OnLightBuildingState(BuildingStates buildingState)
		{
			if (IsNotActive) return;
			
			Game.LastLightUpdate.Value = Game.LastLightUpdate.Value.SetRoomStale(Model.RoomTransform.Id.Value);
			
			if (View.NotVisible) return;
			
			OnViewInitializeLighting();
		}
		#endregion

		#region InteractionModel Events
		void OnToolbarConstruction(Interaction.GenericVector3 interaction)
		{
			if (IsNotActive) return;
			if (Model.BuildingState.Value != BuildingStates.Placing) return;

			switch (interaction.State)
			{
				case Interaction.States.OutOfRange:
					if (View.Visible) Close();
					break;
				case Interaction.States.Idle:
				case Interaction.States.Begin:
				case Interaction.States.Active:
					if (View.NotVisible) Show();
					Model.Transform.Position.Value = interaction.Value.Begin;
					break;
				case Interaction.States.Cancel:
				case Interaction.States.End:
					break;
				default:
					Debug.LogError("Unrecognized Interaction.State: "+interaction.State);
					break;
			}
		}

		void OnToolbarTask(ToolbarModel.Tasks task)
		{
			if (IsNotActive) return;
			if (Model.BuildingState.Value != BuildingStates.Placing) return;
			if (task == ToolbarModel.Tasks.Construction) return;

			Model.PooledState.Value = PooledStates.InActive;
		}
		#endregion
		
		#region NavigationMeshModel Events
		void OnNavigationMeshCalculationState(NavigationMeshModel.CalculationStates calculationState)
		{
			if (calculationState != NavigationMeshModel.CalculationStates.Completed) return;
			if (IsNotActive) return;
			if (Model.BuildingState.Value != BuildingStates.Operating) return;
			
			Model.RecalculateEntrances();
		}
		#endregion
		
		#region BuildingModel Events
		void OnBuildingInventory(Inventory inventory)
		{
			if (IsNotActive) return;
			
			var anyChanged = false;
			var newDesireQuality = Model.DesireQualities.Value.Select(
				d =>
				{
					var result = d.CalculateState(inventory);
					anyChanged |= d.State != result.State;
					return result;
				}
			).ToArray(); // Has to call ToArray otherwise anyChanged will never get called...
			
			if (anyChanged) Model.DesireQualities.Value = newDesireQuality;
		}

		void OnBuildingConstructionInventory(Inventory constructionInventory)
		{
			if (IsNotActive) return;
			if (constructionInventory.IsEmpty || Model.ConstructionInventoryCapacity.Value.IsNotFull(constructionInventory)) return;

			switch (Model.BuildingState.Value)
			{
				case BuildingStates.Constructing:
					Model.BuildingState.Value = BuildingStates.Operating;
					break;
				default:
					Debug.LogError("Tried to fill construction recipe while building is in invalid state: "+Model.BuildingState.Value);
					break;
			}
		}

		void OnBuildingSalvageInventory(Inventory salvageInventory)
		{
			if (IsNotActive) return;
			if (Model.BuildingState.Value != BuildingStates.Salvaging || !salvageInventory.IsEmpty) return;

			Model.PooledState.Value = PooledStates.InActive;
		}

		void OnBuildingState(BuildingStates buildingState)
		{
			if (IsNotActive) return;
			if (View.NotVisible) return;

			View.IsNavigationModified = buildingState == BuildingStates.Operating;

			switch (buildingState)
			{
				case BuildingStates.Placing:
					break;
				case BuildingStates.Constructing:
				case BuildingStates.Operating:
				case BuildingStates.Salvaging:

					if (Model.Light.IsLight.Value)
					{
						if (Model.Light.ReseltLightCalculationsEnabled(Model.IsBuildingState(BuildingStates.Operating)))
						{
							Game.LastLightUpdate.Value = Game.LastLightUpdate.Value.SetRoomStale(Model.RoomTransform.Id.Value);
						}
					}
					else
					{
						Game.LastLightUpdate.Value = Game.LastLightUpdate.Value.SetSensitiveStale(Model.Id.Value);
					}

					if (buildingState == BuildingStates.Operating && Game.NavigationMesh.CalculationState.Value == NavigationMeshModel.CalculationStates.Completed)
					{
						Game.NavigationMesh.QueueCalculation();
					}
					break;
				default:
					Debug.LogError("Unrecognized BuildingState: "+buildingState);
					break;
			}
			
			/*
			if (Game.NavigationMesh.CalculationState.Value == NavigationMeshModel.CalculationStates.Completed)
			{
				Game.NavigationMesh.QueueCalculation();
			}
			*/
		}

		void OnBuildingLightLevel(float lightLevel)
		{
			Model.RecalculateEntrances();
		}

		void OnBuildingOperate(DwellerModel dweller, Desires desire)
		{
			if (IsNotActive) return;
			var quality = Model.DesireQualities.Value.FirstOrDefault(d => d.Desire == desire);

			if (quality.Desire != desire)
			{
				Debug.LogError("Dweller "+dweller.Id.Value+" tried to operate desire "+desire+" on this building, but it doesn't fulfill that");
				return;
			}
			if (quality.State != DesireQuality.States.Available)
			{
				Debug.LogError("Dweller "+dweller.Id.Value+" tried to operate desire "+desire+" on this building, but its state is "+quality.State);
				return;
			}

			if (quality.Cost.IsEmpty) return;

			Model.Inventory.Value -= quality.Cost;
		}
		#endregion
		
		#region ITransform Events
		protected override void OnPosition(Vector3 position)
		{
			base.OnPosition(position);
			if (IsActive && View.Visible) Model.RecalculateEntrances(View);
		}

		protected override void OnRotation(Quaternion rotation)
		{
			base.OnRotation(rotation);
			if (IsActive && View.Visible) Model.RecalculateEntrances(View);
		}
		#endregion
		
		#region View Events
		protected override void OnViewShown()
		{
			Model.RecalculateEntrances(View);
			
			View.IsNavigationModified = Model.BuildingState.Value == BuildingStates.Operating;
			OnViewInitializeLighting();
		}

		void OnViewInitializeLighting()
		{
			if (Model.Light.IsLight.Value)
			{
				switch (Model.BuildingState.Value)
				{
					case BuildingStates.Operating:
						switch (Model.Light.LightState.Value)
						{
							case LightStates.Fueled: View.LightFuelNormal = 1f; break;
							case LightStates.Extinguishing: View.LightFuelNormal = Model.Light.LightFuelInterval.Value.InverseNormalized; break; 
							case LightStates.Extinguished: View.LightFuelNormal = 0f; break;
							default:
								Debug.LogError("Unrecognized LightState: "+Model.Light.LightState.Value+" on "+Model.Id.Value);
								break;
						}
						break;
					case BuildingStates.Decaying:
					case BuildingStates.Salvaging:
						View.LightFuelNormal = 0f;
						break;
				}
			}
		}
		#endregion

		protected override bool QueueNavigationCalculation => Model.IsBuildingState(BuildingStates.Operating);
	}
}