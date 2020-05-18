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
		
		public PrefabPoolModel<ClearableModel> Debris { get; } = new PrefabPoolModel<ClearableModel>();
		public PrefabPoolModel<FloraModel> Flora { get; } = new PrefabPoolModel<FloraModel>();
		public PrefabPoolModel<RoomPrefabModel> Rooms { get; } = new PrefabPoolModel<RoomPrefabModel>();
		public PrefabPoolModel<DoorPrefabModel> Doors { get; } = new PrefabPoolModel<DoorPrefabModel>();
		public PrefabPoolModel<BuildingModel> Buildings { get; } = new PrefabPoolModel<BuildingModel>();

		/// <summary>
		/// The speed modifier for simulated actions, such as movement, build times, etc
		/// </summary>
		[JsonProperty] float simulationMultiplier = 1f;
		[JsonIgnore] public ListenerProperty<float> SimulationMultiplier { get; }
		
		[JsonProperty] float simulationTimeConversion = 1f;
		[JsonIgnore] public ListenerProperty<float> SimulationTimeConversion { get; }
		
		[JsonProperty] DayTime simulationTime = DayTime.Zero;
		[JsonIgnore] public ListenerProperty<DayTime> SimulationTime { get; }

		[JsonProperty] LightDelta lastLightUpdate = LightDelta.Default();
		[JsonIgnore] public ListenerProperty<LightDelta> LastLightUpdate { get; }
		#endregion

		#region NonSerialized
		public NavigationMeshModel NavigationMesh = new NavigationMeshModel();
		[JsonIgnore] public float SimulationDelta => Time.deltaTime;
		[JsonIgnore] public float SimulationTimeDelta => SimulationDelta * SimulationTimeConversion.Value;
		[JsonIgnore] public bool IsSimulationInitialized { get; private set; }
		[JsonIgnore] public IEnumerable<IClearableModel> Clearables =>
			Debris.AllActive
			.Concat(Flora.AllActive);
		
		[JsonIgnore] public IEnumerable<ILightModel> Lights => 
			Buildings.AllActive;

		[JsonIgnore]
		public IEnumerable<ILightSensitiveModel> LightSensitives =>
			Buildings.AllActive
			.Concat<ILightSensitiveModel>(ItemDrops.AllActive)
			.Concat(Clearables);
		#endregion
		
		#region Events
		public event Action SimulationInitialize = ActionExtensions.Empty;
		public Action SimulationUpdate = ActionExtensions.Empty;
		#endregion

		public GameModel()
		{
			SimulationMultiplier = new ListenerProperty<float>(value => simulationMultiplier = value, () => simulationMultiplier);
			SimulationTimeConversion = new ListenerProperty<float>(value => simulationTimeConversion = value, () => simulationTimeConversion);
			SimulationTime = new ListenerProperty<DayTime>(value => simulationTime = value, () => simulationTime);
			LastLightUpdate = new ListenerProperty<LightDelta>(value => lastLightUpdate = value, () => lastLightUpdate);
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
		
		public IEnumerable<RoomPrefabModel> GetOpenAdjacentRooms(params string[] roomIds)
		{
			var results = Rooms.AllActive.Where(r => roomIds.Contains(r.RoomId.Value)).ToList();

			foreach (var door in Doors.AllActive.Where(d => roomIds.Any(d.IsOpenTo)))
			{
				var idToAdd = string.Empty;
				if (results.Any(r => r.RoomId.Value == door.RoomConnection.Value.RoomId0))
				{
					if (results.None(r => r.RoomId.Value == door.RoomConnection.Value.RoomId1)) idToAdd = door.RoomConnection.Value.RoomId1;
					else continue;
				}
				else idToAdd = door.RoomConnection.Value.RoomId0;
				
				results.Add(Rooms.AllActive.First(r => r.RoomId.Value == idToAdd));
			}
			return results;
		}

		public Dictionary<string, List<RoomPrefabModel>> GetOpenAdjacentRoomsMap(params string[] roomIds)
		{
			var result = new Dictionary<string, List<RoomPrefabModel>>();
			foreach (var roomId in roomIds)
			{
				result.Add(
					roomId,
					GetOpenAdjacentRooms(roomId).ToList()
				);
			}
			return result;
		}
	}
}