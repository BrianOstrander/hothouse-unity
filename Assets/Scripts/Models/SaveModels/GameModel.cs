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
		public readonly PoolModel<ItemDropModel> ItemDrops = new PoolModel<ItemDropModel>();
		public readonly PoolModel<DwellerModel> Dwellers = new PoolModel<DwellerModel>();

		[JsonProperty] RoomPrefabModel[] rooms = new RoomPrefabModel[0];
		[JsonIgnore] public readonly ListenerProperty<RoomPrefabModel[]> Rooms;
		
		[JsonProperty] DoorPrefabModel[] doors = new DoorPrefabModel[0];
		[JsonIgnore] public readonly ListenerProperty<DoorPrefabModel[]> Doors;
		
		[JsonProperty] ItemCacheBuildingModel[] itemCaches = new ItemCacheBuildingModel[0];
		[JsonIgnore] public readonly ListenerProperty<ItemCacheBuildingModel[]> ItemCaches;
		
		[JsonProperty] DesireBuildingModel[] desireBuildings = new DesireBuildingModel[0];
		[JsonIgnore] public readonly ListenerProperty<DesireBuildingModel[]> DesireBuildings;

		[JsonProperty] DateTime lastNavigationCalculation;
		[JsonIgnore] public readonly ListenerProperty<DateTime> LastNavigationCalculation;

		/// <summary>
		/// The speed modifier for simulated actions, such as movement, build times, etc
		/// </summary>
		[JsonProperty] float simulationMultiplier = 1f;
		[JsonIgnore] public readonly ListenerProperty<float> SimulationMultiplier;
		
		[JsonProperty] float simulationTimeConversion = 1f;
		[JsonIgnore] public readonly ListenerProperty<float> SimulationTimeConversion;
		
		[JsonProperty] DayTime simulationTime = DayTime.Zero;
		[JsonIgnore] public readonly ListenerProperty<DayTime> SimulationTime;
		#endregion

		#region NonSerialized
		[JsonIgnore] public float SimulationDelta => Time.deltaTime;
		[JsonIgnore] public float SimulationTimeDelta => SimulationDelta * SimulationTimeConversion.Value;
		[JsonIgnore] public bool IsSimulationInitialized { get; private set; }
		public event Action SimulationInitialize = ActionExtensions.Empty;
		public Action SimulationUpdate = ActionExtensions.Empty;
		#endregion

		public GameModel()
		{
			Rooms = new ListenerProperty<RoomPrefabModel[]>(value => rooms = value, () => rooms);
			Doors = new ListenerProperty<DoorPrefabModel[]>(value => doors = value, () => doors);
			ItemCaches = new ListenerProperty<ItemCacheBuildingModel[]>(value => itemCaches = value, () => itemCaches);
			DesireBuildings = new ListenerProperty<DesireBuildingModel[]>(value => desireBuildings = value, () => desireBuildings);
			LastNavigationCalculation = new ListenerProperty<DateTime>(value => lastNavigationCalculation = value, () => lastNavigationCalculation);
			SimulationMultiplier = new ListenerProperty<float>(value => simulationMultiplier = value, () => simulationMultiplier);
			SimulationTimeConversion = new ListenerProperty<float>(value => simulationTimeConversion = value, () => simulationTimeConversion);
			SimulationTime = new ListenerProperty<DayTime>(value => simulationTime = value, () => simulationTime);
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