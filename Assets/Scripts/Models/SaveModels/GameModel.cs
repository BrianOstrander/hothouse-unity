using System;
using System.Linq;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Services;
using Lunra.Satchel;
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
		[JsonProperty] public IdCounter Ids { get; private set; } = new IdCounter();
		[JsonProperty] public ItemStore Items { get; private set; } = new ItemStore();
		
		[JsonProperty] public LevelGenerationModel LevelGeneration { get; private set; } = new LevelGenerationModel();
		[JsonProperty] public WorldCameraModel WorldCamera { get; private set; } = new WorldCameraModel();
		[JsonProperty] public ToolbarModel Toolbar { get; private set; } = new ToolbarModel();
		[JsonProperty] public BuildValidationModel BuildValidation { get; private set; } = new BuildValidationModel();
		[JsonProperty] public EffectsModel Effects { get; private set; } = new EffectsModel();
		[JsonProperty] public HintsModel Hints { get; private set; } = new HintsModel();
		[JsonProperty] public RoomResolverModel RoomResolver { get; private set; } = new RoomResolverModel();
		[JsonProperty] public EventLogModel EventLog { get; private set; } = new EventLogModel();
		[JsonProperty] public JobManageModel JobManage { get; private set; } = new JobManageModel();
		[JsonProperty] public BuildingManageModel BuildingManage { get; private set; } = new BuildingManageModel();
		[JsonProperty] public PopulationModel Population { get; private set; } = new PopulationModel();

		[JsonProperty] public ItemDropPoolModel ItemDrops { get; private set; } = new ItemDropPoolModel();
		[JsonProperty] public RoomPoolModel Rooms { get; private set; } = new RoomPoolModel();
		[JsonProperty] public DoorPoolModel Doors { get; private set; } = new DoorPoolModel();
		[JsonProperty] public DwellerPoolModel Dwellers { get; private set; } = new DwellerPoolModel();
		[JsonProperty] public BubblerPoolModel Bubblers { get; private set; } = new BubblerPoolModel();
		[JsonProperty] public SnapCapPoolModel SnapCaps { get; private set; } = new SnapCapPoolModel();
		[JsonProperty] public DebrisPoolModel Debris { get; private set; } = new DebrisPoolModel();
		[JsonProperty] public BuildingPoolModel Buildings { get; private set; } = new BuildingPoolModel();
		[JsonProperty] public FloraPoolModel Flora { get; private set; } = new FloraPoolModel();
		[JsonProperty] public DecorationPoolModel Decorations { get; private set; } = new DecorationPoolModel();
		[JsonProperty] public GeneratorPoolModel Generators { get; private set; } = new GeneratorPoolModel();

		[JsonProperty] float desireDamageMultiplier = 1f;
		[JsonIgnore] public ListenerProperty<float> DesireDamageMultiplier { get; }
		
		[JsonProperty] float lastNonZeroSimulationMultiplier = 1f;
		[JsonIgnore] public ReadonlyProperty<float> LastNonZeroSimulationMultiplier { get; }
		
		/// <summary>
		/// The speed modifier for simulated actions, such as movement, build times, etc
		/// </summary>
		[JsonProperty] float simulationMultiplier = 1f;
		[JsonIgnore] public ListenerProperty<float> SimulationMultiplier { get; }

		[JsonProperty] DayTime simulationTime = DayTime.Zero;
		readonly ListenerProperty<DayTime> simulationTimeListener;
		[JsonIgnore] public ReadonlyProperty<DayTime> SimulationTime { get; }
		
		[JsonProperty] long simulationTick;
		readonly ListenerProperty<long> simulationTickListener;
		[JsonIgnore] public ReadonlyProperty<long> SimulationTick { get; }
		
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
		[JsonIgnore] public NameGenerator DwellerNames { get; } = new NameGenerator();
		[JsonIgnore] public GameInteractionModel Interaction { get; } = new GameInteractionModel();
		[JsonIgnore] public NavigationMeshModel NavigationMesh = new NavigationMeshModel();
		[JsonIgnore] public float SimulationTimeDelta { get; private set; }
		[JsonIgnore] public bool IsSimulationInitialized { get; private set; }

		[JsonIgnore] public Func<(string RoomId, Vector3 Position, ILightModel[] Except), LightingResult> CalculateMaximumLighting;

		GameCache cache = GameCache.Default();
		readonly ListenerProperty<GameCache> cacheListener;
		[JsonIgnore] public ReadonlyProperty<GameCache> Cache { get; }
		
		bool isSimulating;
		[JsonIgnore] public bool IsSimulating => !Mathf.Approximately(0f, SimulationMultiplier.Value);
		
		[JsonIgnore] public QueryModel Query { get; }
		#endregion
		
		#region Events
		public event Action SimulationInitialize = ActionExtensions.Empty;
		public event Action SimulationUpdate = ActionExtensions.Empty;
		#endregion

		public GameModel()
		{
			DesireDamageMultiplier = new ListenerProperty<float>(value => desireDamageMultiplier = value, () => desireDamageMultiplier);
			
			LastNonZeroSimulationMultiplier = new ReadonlyProperty<float>(
				value => lastNonZeroSimulationMultiplier = value,
				() => lastNonZeroSimulationMultiplier,
				out var lastNonZeroSimulationMultiplierListener
			);
			
			SimulationMultiplier = new ListenerProperty<float>(
				value => simulationMultiplier = value,
				() => simulationMultiplier,
				value =>
				{
					if (!Mathf.Approximately(0f, value)) lastNonZeroSimulationMultiplierListener.Value = value;
				}
			);
			
			SimulationTime = new ReadonlyProperty<DayTime>(
				value => simulationTime = value,
				() => simulationTime,
				out simulationTimeListener
			);
			
			SimulationTick = new ReadonlyProperty<long>(
				value => simulationTick = value,
				() => simulationTick,
				out simulationTickListener
			);

			SimulationPlaytimeElapsed = new ListenerProperty<TimeSpan>(value => simulationPlaytimeElapsed = value, () => simulationPlaytimeElapsed);
			PlaytimeElapsed = new ListenerProperty<TimeSpan>(value => playtimeElapsed = value, () => playtimeElapsed);
			LastLightUpdate = new ListenerProperty<LightDelta>(value => lastLightUpdate = value, () => lastLightUpdate);
			GameResult = new ListenerProperty<GameResult>(value => gameResult = value, () => gameResult);
			
			Cache = new ReadonlyProperty<GameCache>(
				value => cache = value,
				() => cache,
				out cacheListener
			);

			Query = new QueryModel(
				ItemDrops.PoolQuery,
				Rooms.PoolQuery,
				Doors.PoolQuery,
				Dwellers.PoolQuery,
				Bubblers.PoolQuery,
				SnapCaps.PoolQuery,
				Debris.PoolQuery,
				Buildings.PoolQuery,
				Flora.PoolQuery,
				Decorations.PoolQuery,
				Generators.PoolQuery	
			);
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

		public void StepSimulation(float deltaTime)
		{
			SimulationTimeDelta = deltaTime * DayTime.RealTimeToSimulationTime;
			simulationTimeListener.Value += new DayTime(SimulationTimeDelta);
			simulationTickListener.Value++;
			
			SimulationPlaytimeElapsed.Value += TimeSpan.FromSeconds(deltaTime);
			PlaytimeElapsed.Value += TimeSpan.FromSeconds(deltaTime);

			SimulationUpdate();
		}
		
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