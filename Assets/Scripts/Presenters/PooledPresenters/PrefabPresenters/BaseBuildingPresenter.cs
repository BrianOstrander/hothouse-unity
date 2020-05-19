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

			Model.IsLight.Value = View.IsLight;
			
			if (Model.IsLight.Value)
			{
				Model.LightRange.Value = View.LightRange;

				// ILightModel Bindings
				Game.SimulationUpdate += OnLightSimulationUpdate;
				Model.LightState.Changed += OnLightState;
				Model.Inventory.Changed += OnLightBuildingInventory;
				Model.BuildingState.Changed += OnLightBuildingState;
			}

			// Misc Bindings
			Model.Inventory.Changed += OnBuildingInventory;
			Model.ConstructionInventory.Changed += OnBuildingConstructionInventory;
			Model.SalvageInventory.Changed += OnBuildingSalvageInventory;
			Model.Operate += OnBuildingOperate;

			base.Bind();
		}

		protected override void UnBind()
		{
			// ILightModel UnBindings
			Game.SimulationUpdate -= OnLightSimulationUpdate;
			Model.LightState.Changed += OnLightState;
			Model.Inventory.Changed -= OnLightBuildingInventory;
			Model.BuildingState.Changed -= OnLightBuildingState;
			
			// Misc UnBindings
			Model.Inventory.Changed -= OnBuildingInventory;
			Model.ConstructionInventory.Changed -= OnBuildingConstructionInventory;
			Model.SalvageInventory.Changed -= OnBuildingSalvageInventory;
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
			if (Model.PooledState.Value != PooledStates.Active) return;
			if (Model.LightState.Value == LightStates.Extinguished) return;

			Model.LightFuelInterval.Value = Model.LightFuelInterval.Value.Update(Game.SimulationDelta);

			if (Model.LightFuelInterval.Value.IsDone)
			{
				var canRefuel = Model.Inventory.Value.Contains(Model.LightFuel.Value);

				if (canRefuel)
				{
					Model.Inventory.Value -= Model.LightFuel.Value;
					Model.LightFuelInterval.Value = Model.LightFuelInterval.Value.Restarted();
					if (View.Visible) View.LightFuelNormal = 1f;
					return;
				}

				switch (Model.LightState.Value)
				{
					case LightStates.Extinguishing:
						if (View.Visible) View.LightFuelNormal = 0f;
						Model.LightState.Value = LightStates.Extinguished;
						return;
					case LightStates.Fueled:
						Model.LightState.Value = LightStates.Extinguishing;
						Model.LightFuelInterval.Value = Model.LightFuelInterval.Value.Restarted();
						return;
					default:
						Debug.LogError("Unrecognized LightState: "+Model.LightState.Value);
						return;
				}
			}

			switch (Model.LightState.Value)
			{
				case LightStates.Extinguishing:
					if (View.Visible) View.LightFuelNormal = Model.LightFuelInterval.Value.InverseNormalized;
					break;
			}
		}
		
		protected virtual void OnLightState(LightStates lightState)
		{
			Game.LastLightUpdate.Value = Game.LastLightUpdate.Value.SetRoomStale(Model.RoomId.Value);
		}

		protected virtual void OnLightBuildingInventory(Inventory inventory)
		{
			if (Model.LightState.Value != LightStates.Extinguishing) return;
			if (!Model.Inventory.Value.Contains(Model.LightFuel.Value)) return;
			
			Model.Inventory.Value -= Model.LightFuel.Value;
			Model.LightFuelInterval.Value = Model.LightFuelInterval.Value.Restarted();
			if (View.Visible) View.LightFuelNormal = 1f;
			Model.LightState.Value = LightStates.Fueled;
		}
		
		void OnLightBuildingState(BuildingStates buildingState)
		{
			if (View.NotVisible) return;
			
			OnViewInitializeLighting();
		}
		#endregion

		#region Building Events
		void OnBuildingInventory(Inventory inventory)
		{
			var anyChanged = false;
			var newDesireQuality = Model.DesireQuality.Value.Select(
				d =>
				{
					var result = d.CalculateState(inventory);
					anyChanged |= d.State != result.State;
					return result;
				}
			).ToArray(); // Has to call ToArray otherwise anyChanged will never get called...
			
			if (anyChanged) Model.DesireQuality.Value = newDesireQuality;
		}

		void OnBuildingConstructionInventory(Inventory constructionInventory)
		{
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
			if (Model.BuildingState.Value != BuildingStates.Salvaging || !salvageInventory.IsEmpty) return;

			Model.PooledState.Value = PooledStates.InActive;
		}

		void OnBuildingOperate(DwellerModel dweller, Desires desire)
		{
			var quality = Model.DesireQuality.Value.FirstOrDefault(d => d.Desire == desire);

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
		
		#region View Events
		protected override void OnViewShown()
		{
			Model.Entrances.Value = View.Entrances.Select(e => new Entrance(e, Entrance.States.Available)).ToArray();
			OnViewInitializeLighting();
		}

		void OnViewInitializeLighting()
		{
			if (Model.IsLight.Value)
			{
				switch (Model.BuildingState.Value)
				{
					case BuildingStates.Operating:
						switch (Model.LightState.Value)
						{
							case LightStates.Fueled: View.LightFuelNormal = 1f; break;
							case LightStates.Extinguishing: View.LightFuelNormal = Model.LightFuelInterval.Value.InverseNormalized; break; 
							case LightStates.Extinguished: View.LightFuelNormal = 0f; break;
							default:
								Debug.LogError("Unrecognized LightState: "+Model.LightState.Value);
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
	}
}