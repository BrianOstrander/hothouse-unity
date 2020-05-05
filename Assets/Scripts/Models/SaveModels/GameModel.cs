using System;
using Lunra.Core;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;
using Lunra.WildVacuum.Models.AgentModels;
using UnityEngine;

namespace Lunra.WildVacuum.Models
{
	/// <summary>
	/// All data that is serialized about the game.
	/// </summary>
	[SaveModelMeta("games", 0)]
	public class GameModel : SaveModel
	{
		#region Serialized
		public readonly WorldCameraModel WorldCamera = new WorldCameraModel();
		public readonly SelectionModel Selection = new SelectionModel();
		public readonly FloraEffectsModel FloraEffects = new FloraEffectsModel();
		public readonly PoolModel<FloraModel> Flora = new PoolModel<FloraModel>();
		public readonly PoolModel<DwellerModel> Dwellers = new PoolModel<DwellerModel>();

		[JsonProperty] RoomPrefabModel[] rooms = new RoomPrefabModel[0];
		[JsonIgnore] public readonly ListenerProperty<RoomPrefabModel[]> Rooms;
		
		[JsonProperty] DoorPrefabModel[] doors = new DoorPrefabModel[0];
		[JsonIgnore] public readonly ListenerProperty<DoorPrefabModel[]> Doors;
		
		[JsonProperty] ItemCacheBuildingModel[] itemCaches = new ItemCacheBuildingModel[0];
		[JsonIgnore] public readonly ListenerProperty<ItemCacheBuildingModel[]> ItemCaches;

		[JsonProperty] float simulationUpdateMultiplier = 1f;
		[JsonIgnore] public readonly ListenerProperty<float> SimulationUpdateMultiplier;

		[JsonProperty] DateTime lastNavigationCalculation;
		[JsonIgnore] public readonly ListenerProperty<DateTime> LastNavigationCalculation;
		#endregion

		#region NonSerialized
		[JsonIgnore] public bool IsSimulationInitialized { get; private set; }
		public event Action SimulationInitialize = ActionExtensions.Empty;
		public Action<float> SimulationUpdate = ActionExtensions.GetEmpty<float>();
		#endregion

		public GameModel()
		{
			Rooms = new ListenerProperty<RoomPrefabModel[]>(value => rooms = value, () => rooms);
			Doors = new ListenerProperty<DoorPrefabModel[]>(value => doors = value, () => doors);
			ItemCaches = new ListenerProperty<ItemCacheBuildingModel[]>(value => itemCaches = value, () => itemCaches);
			SimulationUpdateMultiplier = new ListenerProperty<float>(value => simulationUpdateMultiplier = value, () => simulationUpdateMultiplier);
			LastNavigationCalculation = new ListenerProperty<DateTime>(value => lastNavigationCalculation = value, () => lastNavigationCalculation);
		}

		public void TriggerSimulationInitialize()
		{
			if (IsSimulationInitialized)
			{
				Debug.LogError("Simulation already initialized...");
				return;
			}

			IsSimulationInitialized = true;

			SimulationInitialize();
		}
	}
}