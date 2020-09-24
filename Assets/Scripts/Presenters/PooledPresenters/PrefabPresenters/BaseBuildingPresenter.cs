using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.Satchel;
using Lunra.StyxMvp.Models;
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

			Model.Boundary.Radius.Value = View.NavigationColliderRadius;
			Model.Light.IsLight.Value = View.IsLight;
			
			Model.Boundary.Contains = View.NavigationCollisionContains;
			Model.Boundary.RandomPoint = generator =>
			{
				Debug.LogError("Not Implimented");
				return null;
			};
			
			if (Model.Light.IsLight.Value)
			{
				Model.Light.LightRange.Value = View.LightRange;
				Model.Light.ReseltLightCalculationsEnabled(Model.IsBuildingState(BuildingStates.Operating));

				// ILightModel Bindings
				Game.SimulationUpdate += OnLightSimulationUpdate;
				Model.Light.LightState.Changed += OnLightState;
				// Model.Inventory.All.Changed += OnLightBuildingInventory;
				Debug.LogWarning("TODO: Light Handling Bind - prolly wanna move this logic to the component instead");
				Model.BuildingState.Changed += OnLightBuildingState;
			}

			// Misc Bindings
			Game.Toolbar.ConstructionTranslation.Changed += OnToolbarConstructionTranslation;
			Game.Toolbar.ConstructionRotation.Changed += OnToolbarConstructionRotation;
			Game.Toolbar.Task.Changed += OnToolbarTask;
			Game.NavigationMesh.CalculationState.Changed += OnNavigationMeshCalculationState;

			Model.Inventory.Container.Updated += OnInventoryContainerUpdated;
			Model.BuildingState.Changed += OnBuildingState;
			Model.LightSensitive.LightLevel.Changed += OnBuildingLightLevel;
			Model.Health.Current.Changed += OnBuildingHealthCurrent;
			Model.Obligations.AddCallback(
				ObligationCategories.Construct.Assemble,
				OnObligationsConstructAssemble
			);
			Model.Obligations.AddCallback(
				ObligationCategories.Craft.Recipe,
				OnObligationsCraftRecipe
			);

			base.Bind();
		}

		protected override void UnBind()
		{
			Model.Boundary.Contains = null;
			Model.Boundary.RandomPoint = null;

			// ILightModel UnBindings
			Game.SimulationUpdate -= OnLightSimulationUpdate;
			Model.Light.LightState.Changed -= OnLightState;
			// Model.Inventory.All.Changed -= OnLightBuildingInventory;
			Debug.LogWarning("TODO: Light Handling UnBind - see bind for details");
			Model.BuildingState.Changed -= OnLightBuildingState;
			
			// Misc UnBindings
			Game.Toolbar.ConstructionTranslation.Changed -= OnToolbarConstructionTranslation;
			Game.Toolbar.ConstructionRotation.Changed -= OnToolbarConstructionRotation;
			Game.Toolbar.Task.Changed -= OnToolbarTask;
			Game.NavigationMesh.CalculationState.Changed -= OnNavigationMeshCalculationState;
			
			Model.Inventory.Container.Updated -= OnInventoryContainerUpdated;
			Model.BuildingState.Changed -= OnBuildingState;
			Model.LightSensitive.LightLevel.Changed -= OnBuildingLightLevel;
			Model.Health.Current.Changed -= OnBuildingHealthCurrent;
			Model.Obligations.RemoveCallback(
				ObligationCategories.Construct.Assemble,
				OnObligationsConstructAssemble
			);
			Model.Obligations.RemoveCallback(
				ObligationCategories.Craft.Recipe,
				OnObligationsCraftRecipe
			);

			base.UnBind();
		}

		protected override void OnSimulationInitialized()
		{
			Debug.LogWarning("TODO: Available inventory or something");
			// OnBuildingAvailableInventory(Model.Inventory.All.Value);
		}
		
		#region LightSourceModel Events
		protected virtual void OnLightSimulationUpdate()
		{
			if (Model.BuildingState.Value != BuildingStates.Operating) return;
			if (Model.Light.LightState.Value == LightStates.Extinguished) return;

			Model.Light.LightFuelInterval.Value = Model.Light.LightFuelInterval.Value.Update(Game.SimulationTimeDelta);

			if (Model.Light.LightFuelInterval.Value.IsDone)
			{
				// var canRefuel = Model.Inventory.Available.Value.Contains(Model.Light.LightFuel.Value);
				Debug.LogWarning("TODO: Some refuel testing logic here");
				var canRefuel = false;

				if (canRefuel)
				{
					// Model.Inventory.Remove(Model.Light.LightFuel.Value);
					Debug.LogError("TODO: Removing of fuel");
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
						// TODO SHOULD BE A ERROR EVENTUALLY
						Debug.LogWarning("Unrecognized LightState: "+Model.Light.LightState.Value);
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
			Game.LastLightUpdate.Value = Game.LastLightUpdate.Value.SetRoomStale(Model.RoomTransform.Id.Value);
			
			switch (lightState)
			{
				case LightStates.Extinguished:
					Model.BuildingState.Value = BuildingStates.Salvaging;
					break;
			}
		}

		// protected virtual void OnLightBuildingInventory(Inventory inventory)
		// {
		// 	if (Model.Light.LightState.Value != LightStates.Extinguishing) return;
		// 	if (!Model.Inventory.Available.Value.Contains(Model.Light.LightFuel.Value)) return;
		// 	
		// 	Model.Inventory.Remove(Model.Light.LightFuel.Value);
		// 	Model.Light.LightFuelInterval.Value = Model.Light.LightFuelInterval.Value.Restarted();
		// 	if (View.Visible) View.LightFuelNormal = 1f;
		// 	Model.Light.LightState.Value = LightStates.Fueled;
		// }
		
		void OnLightBuildingState(BuildingStates buildingState)
		{
			Game.LastLightUpdate.Value = Game.LastLightUpdate.Value.SetRoomStale(Model.RoomTransform.Id.Value);
			
			if (View.NotVisible) return;
			
			OnViewInitializeLighting();
		}
		#endregion

		#region InteractionModel Events
		void OnToolbarConstructionTranslation(Interaction.RoomVector3 interaction)
		{
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

		void OnToolbarConstructionRotation(Interaction.GenericFloat interaction)
		{
			if (Model.BuildingState.Value != BuildingStates.Placing) return;
			if (View.NotVisible) return;
			
			switch (interaction.State)
			{
				case Interaction.States.Idle:
				case Interaction.States.Begin:
				case Interaction.States.Active:
					Model.Transform.Rotation.Value *= interaction.Value.GetRotation(Vector3.up);
					break;
			}
		}

		void OnToolbarTask(ToolbarModel.Tasks task)
		{
			if (Model.BuildingState.Value != BuildingStates.Placing) return;
			if (task == ToolbarModel.Tasks.Construction) return;

			Model.PooledState.Value = PooledStates.InActive;
		}
		#endregion
		
		#region NavigationMeshModel Events
		void OnNavigationMeshCalculationState(NavigationMeshModel.CalculationStates calculationState)
		{
			if (calculationState != NavigationMeshModel.CalculationStates.Completed) return;
			if (Model.BuildingState.Value != BuildingStates.Operating) return;

			Model.RecalculateEntrances();
			Model.Activities.CalculateRestrictions();
		}
		#endregion

		#region IObligationModel Events
		void OnObligationsConstructAssemble(
			Obligation obligation,
			IModel source
		)
		{
			Model.BuildingState.Value = BuildingStates.Operating;
		}
		
		void OnObligationsCraftRecipe(
			Obligation obligation,
			IModel source
		)
		{
			// Not sure we need this? It does stop an unhandled obligation error from appearing though...
		}
		#endregion
		
		#region BuildingModel Events
		void OnInventoryContainerUpdated(Container.Event containerEvent)
		{
			void trigger(
				string capacityPoolType,
				Action<Container.Event, Item> callback
			)
			{
				if (!Model.Inventory.Container.TryFindFirst(i => i[Items.Keys.CapacityPool.Type] == capacityPoolType, out var capacityPool))
				{
					Debug.LogError($"Cannot find capacity pool of type {capacityPoolType} in {Model.Inventory}");
					return;
				}

				if (capacityPool[Items.Keys.CapacityPool.IsForbidden]) return;
				
				callback(containerEvent, capacityPool);
			}
			
			switch (Model.BuildingState.Value)
			{
				case BuildingStates.Placing:
					break;
				case BuildingStates.Constructing:
					trigger(
						Items.Values.CapacityPool.Types.Construction,
						OnConstructionInventoryContainerUpdated
					);
					break;
				case BuildingStates.Operating:
					trigger(
						Items.Values.CapacityPool.Types.Stockpile,
						OnOperatingInventoryContainerUpdated
					);
					break;
				case BuildingStates.Salvaging:
					trigger(
						Items.Values.CapacityPool.Types.Salvage,
						OnSalvagingInventoryContainerUpdated
					);
					break;
				default:
					Debug.LogError($"Unrecognized building state: {Model.BuildingState.Value}");
					break;
			}
		}
		
		void OnConstructionInventoryContainerUpdated(
			Container.Event containerEvent,
			Item capacityPool
		)
		{
			var countTarget = capacityPool[Items.Keys.CapacityPool.CountTarget];
			if (capacityPool[Items.Keys.CapacityPool.CountCurrent] < countTarget) return;
		
			var countCurrent = 0;
			foreach (var resource in Model.Inventory.Container.All(i => i[Items.Keys.Resource.CapacityPoolId] == capacityPool.Id))
			{
				countCurrent += resource.Stack.Count;
				if (countTarget == countCurrent) break;
			}

			// TODO: There should be a construction operation that happens here first...
			if (countTarget <= countCurrent) OnInventoryContainerTransition(capacityPool, BuildingStates.Operating);
		}
		
		void OnOperatingInventoryContainerUpdated(
			Container.Event containerEvent,
			Item capacityPool
		)
		{
			
		}
		
		void OnSalvagingInventoryContainerUpdated(
			Container.Event containerEvent,
			Item capacityPool
		)
		{
			
		}

		void OnInventoryContainerTransition(
			Item capacityPool,
			BuildingStates buildStateNew
		)
		{
			Model.BuildingState.Value = buildStateNew;
			Model.Inventory.SetForbidden(capacityPool.Id, true);

			string capacityPoolType;

			switch (buildStateNew)
			{
				case BuildingStates.Placing:
					return;
				case BuildingStates.Constructing:
					capacityPoolType = Items.Values.CapacityPool.Types.Construction;
					break;
				case BuildingStates.Operating:
					capacityPoolType = Items.Values.CapacityPool.Types.Stockpile;
					break;
				case BuildingStates.Salvaging:
					capacityPoolType = Items.Values.CapacityPool.Types.Salvage;
					break;
				default:
					Debug.LogError($"Unrecognized build state: {buildStateNew}");
					return;
			}
			
			if (!Model.Inventory.Container.TryFindFirst(i => i[Items.Keys.CapacityPool.Type] == capacityPoolType, out capacityPool))
			{
				Debug.LogError($"Cannot find capacity pool of type {Items.Values.CapacityPool.Types.Stockpile}");
				return;
			}
			
			Model.Inventory.SetForbidden(capacityPool.Id, false);
		}
		
		// void OnBuildingConstructionInventory(Inventory constructionInventory)
		// {
		// 	if (constructionInventory.IsEmpty || Model.ConstructionInventory.AllCapacity.Value.IsNotFull(constructionInventory)) return;
		//
		// 	switch (Model.BuildingState.Value)
		// 	{
		// 		case BuildingStates.Constructing:
		// 			Model.Obligations.Add(ObligationCategories.Construct.Assemble);
		// 			// Model.BuildingState.Value = BuildingStates.Operating;
		// 			break;
		// 		default:
		// 			Debug.LogError("Tried to fill construction recipe while building is in invalid state: "+Model.BuildingState.Value);
		// 			break;
		// 	}
		// }
		//
		// void OnBuildingSalvageInventory(Inventory salvageInventory)
		// {
		// 	if (Model.BuildingState.Value != BuildingStates.Salvaging || !salvageInventory.IsEmpty) return;
		//
		// 	Model.PooledState.Value = PooledStates.InActive;
		// }
		//
		// void OnBuildingAvailableInventory(Inventory availableInventory)
		// {
		// 	Model.Activities.CalculateRestrictions();
		// }

		void OnBuildingState(BuildingStates buildingState)
		{
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

					if (buildingState == BuildingStates.Constructing)
					{
						Debug.LogError("TODO: Some kinda cleanup logic here");
						// if (Model.Inventory.IsFull())
						// {
						// 	Model.Obligations.Add(ObligationCategories.Construct.Assemble);
						// }
						// else
						// {
						// 	foreach (var constructionItem in Model.ConstructionInventory.AvailableCapacity.Value.GetMaximum().Entries)
						// 	{
						// 		if (constructionItem.Weight == 0) continue;
						// 		Model.ConstructionInventory.requ
						// 	}
						// }
					}
					else if (Game.NavigationMesh.CalculationState.Value == NavigationMeshModel.CalculationStates.Completed)
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

		void OnBuildingHealthCurrent(float current)
		{
			// TODO: Should use their own hurt particle, not plants...
			// Game.Effects.HurtQueue.Enqueue(new EffectsModel.Request(Model.Transform.Position.Value));
			
			if (!Mathf.Approximately(0f, current)) return;

			switch (Model.BuildingState.Value)
			{
				case BuildingStates.Constructing:
				case BuildingStates.Operating:
					Model.BuildingState.Value = BuildingStates.Salvaging;
					break;
				default:
					Debug.LogError("Unrecognized BuildingState: "+Model.BuildingState.Value);
					return;
			}
		}
		#endregion
		
		#region ITransform Events
		protected override void OnPosition(Vector3 position)
		{
			base.OnPosition(position);
			if (View.Visible) Model.RecalculateEntrances(View);
		}

		protected override void OnRotation(Quaternion rotation)
		{
			base.OnRotation(rotation);
			if (View.Visible) Model.RecalculateEntrances(View);
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
								// TODO THIS SHOULD BE A ERROR EVENTUALLY
								Debug.LogWarning("Unrecognized LightState: "+Model.Light.LightState.Value+" on "+Model.Id.Value);
								break;
						}
						break;
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