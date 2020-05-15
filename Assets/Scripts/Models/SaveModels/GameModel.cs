using System;
using System.Linq;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Models.AgentModels;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	/// <summary>
	/// All data that is serialized about the game.
	/// </summary>
	[SaveModelMeta("games", 0)]
	public class GameModel : SaveModel
	{
		#region Serialized
		public WorldCameraModel WorldCamera { get; } = new WorldCameraModel();
		public SelectionModel Selection  { get; } = new SelectionModel();
		public FloraEffectsModel FloraEffects { get; } = new FloraEffectsModel();
		
		public PoolModel<ItemDropModel> ItemDrops { get; } = new PoolModel<ItemDropModel>();
		public PoolModel<DwellerModel> Dwellers { get; } = new PoolModel<DwellerModel>();
		
		public PrefabPoolModel<FloraModel> Flora { get; } = new PrefabPoolModel<FloraModel>();
		public PrefabPoolModel<RoomPrefabModel> Rooms { get; } = new PrefabPoolModel<RoomPrefabModel>();
		public PrefabPoolModel<DoorPrefabModel> Doors { get; } = new PrefabPoolModel<DoorPrefabModel>();
		public PrefabPoolModel<BuildingModel> Buildings { get; } = new PrefabPoolModel<BuildingModel>();
		
		[JsonProperty] DateTime lastNavigationCalculation;
		[JsonIgnore] public ListenerProperty<DateTime> LastNavigationCalculation { get; }

		/// <summary>
		/// The speed modifier for simulated actions, such as movement, build times, etc
		/// </summary>
		[JsonProperty] float simulationMultiplier = 1f;
		[JsonIgnore] public ListenerProperty<float> SimulationMultiplier { get; }
		
		[JsonProperty] float simulationTimeConversion = 1f;
		[JsonIgnore] public ListenerProperty<float> SimulationTimeConversion { get; }
		
		[JsonProperty] DayTime simulationTime = DayTime.Zero;
		[JsonIgnore] public ListenerProperty<DayTime> SimulationTime { get; }
		#endregion

		#region NonSerialized
		[JsonIgnore] public float SimulationDelta => Time.deltaTime;
		[JsonIgnore] public float SimulationTimeDelta => SimulationDelta * SimulationTimeConversion.Value;
		[JsonIgnore] public bool IsSimulationInitialized { get; private set; }
		[JsonIgnore] public IEnumerable<IClearableModel> Clearables => Flora.AllActive;
		#endregion
		
		#region Events
		public event Action SimulationInitialize = ActionExtensions.Empty;
		public Action SimulationUpdate = ActionExtensions.Empty;
		#endregion

		public GameModel()
		{
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