using System;
using System.Linq;
using System.Collections.Generic;
using Lunra.Core;
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
		public ToolbarModel Toolbar { get; } = new ToolbarModel();
		public BuildValidationModel BuildValidation { get; } = new BuildValidationModel();
		public EffectsModel Effects { get; } = new EffectsModel();
		public HintsModel Hints { get; } = new HintsModel();
		public RoomResolverModel RoomResolver { get; } = new RoomResolverModel();
		public EventLogModel EventLog { get; } = new EventLogModel();
		public JobManageModel JobManage { get; } = new JobManageModel();

		public GenericPrefabPoolModel<ItemDropModel> ItemDrops { get; } = new GenericPrefabPoolModel<ItemDropModel>();
		public RoomPoolModel Rooms { get; } = new RoomPoolModel();
		public DoorPoolModel Doors { get; } = new DoorPoolModel();
		public DwellerPoolModel Dwellers { get; } = new DwellerPoolModel();
		public DebrisPoolModel Debris { get; } = new DebrisPoolModel();
		public BuildingPoolModel Buildings { get; } = new BuildingPoolModel();
		public FloraPoolModel Flora { get; } = new FloraPoolModel();
		public ObligationIndicatorPoolModel ObligationIndicators { get; } = new ObligationIndicatorPoolModel();
		public SeekerPoolModel Seekers { get; } = new SeekerPoolModel();

		[JsonProperty] float desireDamageMultiplier = 1f;
		[JsonIgnore] public ListenerProperty<float> DesireDamageMultiplier { get; }
		/// <summary>
		/// The speed modifier for simulated actions, such as movement, build times, etc
		/// </summary>
		[JsonProperty] float simulationMultiplier = 1f;
		[JsonIgnore] public ListenerProperty<float> SimulationMultiplier { get; }
		
		[JsonProperty] float simulationTimeConversion = 1f;
		[JsonIgnore] public ListenerProperty<float> SimulationTimeConversion { get; }
		
		[JsonProperty] DayTime simulationTime = DayTime.Zero;
		[JsonIgnore] public ListenerProperty<DayTime> SimulationTime { get; }
		
		[JsonProperty] TimeSpan simulationPlaytimeElapsed;
		[JsonIgnore] public ListenerProperty<TimeSpan> SimulationPlaytimeElapsed { get; }
		
		[JsonProperty] TimeSpan playtimeElapsed;
		[JsonIgnore] public ListenerProperty<TimeSpan> PlaytimeElapsed { get; }

		[JsonProperty] LightDelta lastLightUpdate = LightDelta.Default();
		[JsonIgnore] public ListenerProperty<LightDelta> LastLightUpdate { get; }

		[JsonProperty] GameResult gameResult = Models.GameResult.Default();
		[JsonIgnore] public ListenerProperty<GameResult> GameResult { get; }
		#endregion

		#region NonSerialized
		[JsonIgnore] public GameInteractionModel Interaction { get; } = new GameInteractionModel();
		[JsonIgnore] public NavigationMeshModel NavigationMesh = new NavigationMeshModel();
		[JsonIgnore] public float SimulationDelta => Time.deltaTime;
		[JsonIgnore] public float SimulationTimeDelta => SimulationDelta * SimulationTimeConversion.Value;
		[JsonIgnore] public bool IsSimulationInitialized { get; private set; }

		public IEnumerable<IClearableModel> GetClearables()
		{
			return Debris.AllActive
				.Concat(Flora.AllActive);
		}

		public IEnumerable<ILightModel> GetLightsActive()
		{
			return GetLights(m => m.Light.IsLightActive());
		}
		
		public IEnumerable<ILightModel> GetLights(Func<ILightModel, bool> predicate = null)
		{
			predicate = predicate ?? (m => true);
			return Buildings.AllActive.Where(m => m.Light.IsLight.Value && predicate(m));	
		}

		public IEnumerable<IEnterableModel> GetEnterablesAvailable()
		{
			return GetEnterables(m => m.Enterable.Entrances.Value.Any(e => e.State == Entrance.States.Available));
		}
		
		public IEnumerable<IEnterableModel> GetEnterables(Func<IEnterableModel, bool> predicate = null)
		{
			predicate = predicate ?? (m => true);
			
			return Buildings.AllActive.Where(predicate)
				.Concat(Doors.AllActive.Where(predicate));
		}

		public IEnumerable<IObligationModel> GetObligationsAvailable()
		{
			return GetObligations(m => m.Obligations.All.Value.Any(o => o.State == Obligation.States.Available));
		}
		
		public IEnumerable<IObligationModel> GetObligations(Func<IObligationModel, bool> predicate = null)
		{
			predicate = predicate ?? (m => true);
			return Doors.AllActive.Where(predicate);
		}

		public IEnumerable<ILightSensitiveModel> GetLightSensitives()
		{
			return Buildings.AllActive
				.Concat<ILightSensitiveModel>(ItemDrops.AllActive)
				.Concat(Doors.AllActive)
				.Concat(GetClearables());
		}

		[JsonIgnore] public Func<(string RoomId, Vector3 Position, ILightModel[] Except), LightingResult> CalculateMaximumLighting;

		GameCache cache = GameCache.Default();
		readonly ListenerProperty<GameCache> cacheListener;
		[JsonIgnore] public ReadonlyProperty<GameCache> Cache { get; }
		
		bool isSimulating;
		[JsonIgnore] public ListenerProperty<bool> IsSimulating { get; }
		#endregion
		
		#region Events
		public event Action SimulationInitialize = ActionExtensions.Empty;
		[JsonIgnore] public Action SimulationUpdate = ActionExtensions.Empty;
		#endregion

		public GameModel()
		{
			DesireDamageMultiplier = new ListenerProperty<float>(value => desireDamageMultiplier = value, () => desireDamageMultiplier);
			SimulationMultiplier = new ListenerProperty<float>(value => simulationMultiplier = value, () => simulationMultiplier);
			SimulationTimeConversion = new ListenerProperty<float>(value => simulationTimeConversion = value, () => simulationTimeConversion);
			SimulationTime = new ListenerProperty<DayTime>(value => simulationTime = value, () => simulationTime);
			SimulationPlaytimeElapsed = new ListenerProperty<TimeSpan>(value => simulationPlaytimeElapsed = value, () => simulationPlaytimeElapsed);
			PlaytimeElapsed = new ListenerProperty<TimeSpan>(value => playtimeElapsed = value, () => playtimeElapsed);
			LastLightUpdate = new ListenerProperty<LightDelta>(value => lastLightUpdate = value, () => lastLightUpdate);
			GameResult = new ListenerProperty<GameResult>(value => gameResult = value, () => gameResult);
			
			Cache = new ReadonlyProperty<GameCache>(
				value => cache = value,
				() => cache,
				out cacheListener
			);
			IsSimulating = new ListenerProperty<bool>(value => isSimulating = value, () => isSimulating);
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

		public void ResetSimulationInitialized() => IsSimulationInitialized = false;

		public void InitializeCache()
		{
			// This is a little broken but ok...
			CalculateCache();
			CalculateCache();
		}
		public void CalculateCache() => cacheListener.Value = cacheListener.Value.Calculate(this);

		public Dictionary<string, bool> GetAdjacentRooms(string roomId)
		{
			var result = new Dictionary<string, bool>();

			var allRoomIds = Doors.AllActive
				.Where(d => d.IsConnnecting(roomId))
				.SelectMany(d => new[] {d.RoomConnection.Value.RoomId0, d.RoomConnection.Value.RoomId1})
				.Distinct();

			foreach (var currentRoomId in allRoomIds)
			{
				if (currentRoomId == roomId) continue;
				
				result.Add(
					currentRoomId,
					Doors.AllActive.Any(d => d.IsOpenBetween(roomId, currentRoomId))
				);
			}

			return result;
		}
		
		public IEnumerable<RoomModel> GetOpenAdjacentRooms(params string[] roomIds)
		{
			var results = Rooms.AllActive.Where(r => roomIds.Contains(r.RoomTransform.Id.Value)).ToList();

			foreach (var door in Doors.AllActive.Where(d => roomIds.Any(d.IsOpenTo)))
			{
				var idToAdd = string.Empty;
				if (results.Any(r => r.RoomTransform.Id.Value == door.RoomConnection.Value.RoomId0))
				{
					if (results.None(r => r.RoomTransform.Id.Value == door.RoomConnection.Value.RoomId1)) idToAdd = door.RoomConnection.Value.RoomId1;
					else continue;
				}
				else idToAdd = door.RoomConnection.Value.RoomId0;
				
				results.Add(Rooms.AllActive.First(r => r.RoomTransform.Id.Value == idToAdd));
			}
			return results;
		}

		public Dictionary<string, List<RoomModel>> GetOpenAdjacentRoomsMap(params string[] roomIds)
		{
			var result = new Dictionary<string, List<RoomModel>>();
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