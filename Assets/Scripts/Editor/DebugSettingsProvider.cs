using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lunra.Core;
using Lunra.Editor.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Services;
using Lunra.Hothouse.Services.Editor;
using Lunra.Satchel;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Models;
using Lunra.StyxMvp.Services;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Inventory = Lunra.Satchel.Inventory;

namespace Lunra.Hothouse.Editor
{
	public static class DebugSettings
	{
		public enum MainMenuBehaviours
		{
			Unknown = 0,
			None = 10,
			CreateNewGame = 20,
			LoadRecentGame = 30
		}
		
		const string KeyPrefix = SettingsProviderStrings.ProjectKeyPrefix + "DebugSettings.";

		public static EditorPrefsBool AutoRevealRooms { get; } = new EditorPrefsBool(KeyPrefix + "AutoRevealRooms");
		public static EditorPrefsBool LogGameOverAnalysis { get; } = new EditorPrefsBool(KeyPrefix + "LogGameOverAnalysis");
		public static EditorPrefsBool GameOverOnDwellerDeath { get; } = new EditorPrefsBool(KeyPrefix + "GameOverOnDwellerDeath");
		public static EditorPrefsEnum<MainMenuBehaviours> MainMenuBehaviour { get; } = new EditorPrefsEnum<MainMenuBehaviours>(KeyPrefix + "MainMenuBehaviour", MainMenuBehaviours.None);
		public static EditorPrefsBool SeedOverrideEnabled { get; } = new EditorPrefsBool(KeyPrefix + "SeedOverrideEnabled");
		public static EditorPrefsInt SeedOverride { get; } = new EditorPrefsInt(KeyPrefix + "SeedOverride");
	}
	
	public class DebugSettingsProvider : SettingsProvider
	{
		struct GameOverAnalysis
		{
			public string Id;
			public GameResult Result;
			public string Summary;

			public DayTime SurvivalMinimum;
			public DayTime SurvivalMaximum;
			public DayTime SurvivalAverage;

			public TimeSpan PlaytimeElapsed;
		}
		
		static class Content
		{
			public static GUIContent OpenDebugSettingsProvider = new GUIContent("Open Debug Settings Provider");
			public static GUIContent OpenSaveLocation = new GUIContent("Open save location");
			public static GUIContent SaveAndCopySerializedGameToClipboard = new GUIContent("Save and copy serialized game to clipboard");
			public static GUIContent StartNewGame = new GUIContent("Start New Game");
			public static GUIContent ReloadGame = new GUIContent("Reload Game");
			public static GUIContent SaveAndReloadGame = new GUIContent("Save & Reload Game");
			public static GUIContent QueueNavigationCalculation = new GUIContent("Queue navigation calculation");
			public static GUIContent RevealAllRooms = new GUIContent("Reveal All Rooms");
			public static GUIContent OpenAllDoors = new GUIContent("Open All Doors");
			public static GUIContent SimulationSpeedReset = new GUIContent("Reset");
			public static GUIContent SimulationSpeedIncrease = new GUIContent("->", "Increase");
			public static GUIContent SimulationSpeedDecrease = new GUIContent("<-", "Decrease");

			public static GUIContent ValidateSerializedProperties = new GUIContent("Validate Serialized Properties");
		}

		string lastAutoRevealedRoomsForGameId;
		int previousDwellerCount = int.MinValue;
		Stack<GameOverAnalysis> gameOverAnalyses = new Stack<GameOverAnalysis>();
		bool hasMainMenuIdleBeenHandled;
		Action<MainMenuModel> onMainMenuIdle;

		public DebugSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope)
		{
			EditorApplication.update -= OnUpdate;
			EditorApplication.update += OnUpdate;

			App.Instantiated -= OnAppInstantiated;
			App.Instantiated += OnAppInstantiated;
		}
		
		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider()
		{
			var provider = new DebugSettingsProvider("Hothouse/Debug");

			provider.keywords = new []
			{
				DebugSettings.AutoRevealRooms.LabelName,
				DebugSettings.LogGameOverAnalysis.LabelName,
				DebugSettings.MainMenuBehaviour.LabelName,
				DebugSettings.SeedOverrideEnabled.LabelName,
				DebugSettings.SeedOverride.LabelName,
				
				Content.OpenDebugSettingsProvider.text,
				Content.OpenSaveLocation.text,
				Content.SaveAndCopySerializedGameToClipboard.text,
				Content.StartNewGame.text,
				Content.ReloadGame.text,
				Content.QueueNavigationCalculation.text,
				Content.RevealAllRooms.text,
				Content.OpenAllDoors.text,
				Content.SimulationSpeedReset.text,
				Content.SimulationSpeedIncrease.text,
				Content.SimulationSpeedDecrease.text,
				Content.ValidateSerializedProperties.text
			};
			
			return provider;
		}

		public override void OnGUI(string searchContext)
		{
			if (GUILayout.Button(Content.OpenDebugSettingsProvider)) OpenSettingsProviderAsset();
			if (GUILayout.Button(Content.OpenSaveLocation)) EditorUtility.RevealInFinder(Application.persistentDataPath);

			var isInGame = GameStateEditorUtility.GetGame(out var gameModel, out _);
			
			GUIExtensions.PushEnabled(isInGame);
			{
				if (GUILayout.Button(Content.SaveAndCopySerializedGameToClipboard)) App.M.Save(gameModel, OnSaveAndCopySerializedGameToClipboard);
				if (GUILayout.Button(Content.QueueNavigationCalculation)) gameModel.NavigationMesh.QueueCalculation();

				GUILayout.BeginHorizontal();
				{
					GUIExtensions.PushEnabled(true, true);
					{
						DebugSettings.AutoRevealRooms.Draw(GUILayout.ExpandWidth(false));
					}
					GUIExtensions.PopEnabled();

					if (GUILayout.Button(Content.RevealAllRooms)) RevealAllRooms(gameModel);

					if (GUILayout.Button(Content.OpenAllDoors))
					{
						foreach (var room in gameModel.Rooms.AllActive) room.IsRevealed.Value = true;
						foreach (var door in gameModel.Doors.AllActive) door.IsOpen.Value = true;
					}
				}
				GUILayout.EndHorizontal();
				
				GUIExtensions.PushEnabled(true, true);
				{
					DebugSettings.MainMenuBehaviour.Draw();
				}
				GUIExtensions.PopEnabled();
				
				GUILayout.BeginHorizontal();
				{
					if (GUILayout.Button(Content.StartNewGame)) TriggerMainMenuStartNewGame();
					
					if (GUILayout.Button(Content.ReloadGame)) TriggerMainMenuReloadGame(gameModel);
					
					if (GUILayout.Button(Content.SaveAndReloadGame)) TriggerMainMenuSaveAndReloadGame(gameModel);
				}
				GUILayout.EndHorizontal();
				
				GUILayout.BeginHorizontal();
				{
					GUIExtensions.PushEnabled(true, true);
					{
						var seedOverrideEnabled = DebugSettings.SeedOverrideEnabled.Draw(GUILayout.ExpandWidth(false));
						
						GUIExtensions.PushContentColor(seedOverrideEnabled ? Color.white : Color.gray);
						{
							DebugSettings.SeedOverride.DrawValue();
						}
						GUIExtensions.PopContentColor();
					}
					GUIExtensions.PopEnabled();
					
					GUIExtensions.PushEnabled(false);
					{
						EditorGUILayout.IntField(isInGame ? gameModel.LevelGeneration.Seed.Value : 0);
					}
					GUIExtensions.PopEnabled();
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("Simulation Speed", GUILayout.ExpandWidth(false));
					GUILayout.Label($"{(isInGame ? gameModel.SimulationMultiplier.Value : 0f):N0}x", GUILayout.Width(32f));
					if (GUILayout.Button(Content.SimulationSpeedReset)) gameModel.SimulationMultiplier.Value = 1f;
					if (GUILayout.Button(Content.SimulationSpeedDecrease)) gameModel.SimulationMultiplier.Value = Mathf.Max(0f, gameModel.SimulationMultiplier.Value - 1f);
					if (GUILayout.Button(Content.SimulationSpeedIncrease)) gameModel.SimulationMultiplier.Value++;
				}
				GUILayout.EndHorizontal();
				
				DebugSettings.LogGameOverAnalysis.Draw();
				DebugSettings.GameOverOnDwellerDeath.Draw();
			}
			GUIExtensions.PopEnabled();
			
			if (GUILayout.Button(Content.ValidateSerializedProperties))
			{
				SerializedPropertiesUtility.Validate();
			}
			
			GUILayout.Label("Scratch Area", EditorStyles.boldLabel);

			if (GUILayout.Button("Validate Promise Components"))
			{
				var dweller = gameModel.Dwellers.FirstActive(d => d.IsDebugging);

				var res = "ObPromis on ob: " + dweller.ObligationPromises.Id.Value;
				res += "\nObPromis in list: " + dweller.Components.First(c => c is ObligationPromiseComponent).Id.Value;

				if (dweller.ObligationPromises.All.TryPeek(out var obPromis))
				{
					res += "\n" + obPromis;
				}
				else
				{
					res += "\nNot found";
				}
				
				Debug.Log(res);
			}

			if (GUILayout.Button("item cap"))
			{
				var cap = Items.Instantiate.Capacity
					.Of(
						Items.Values.Resource.Ids.Stalk,
						10
					);
				
				Debug.Log(cap.Aggregate("uh: ", (r, c) => $"{r}\n{c.Property.ToString(c.Key)}"));
			}

			if (GUILayout.Button("Test Satchel 0f"))
			{
				var itemStore = new ItemStore();
				itemStore.Initialize(new IdCounter());

				itemStore.Updated += updateEvent =>
				{
					var res = "\n-------- itemStore.Updated --------\n"; 
					res += updateEvent.ToString(ItemStore.Event.Formats.IncludeProperties);
					res += "\n-------- All Items --------\n";
					res += itemStore.ToString(true, true);
				
					Debug.Log(res);
				};
				
				var inventory0 = itemStore.Builder.Inventory();
				
				inventory0.Updated += updateInventoryEvent =>
				{
					var res = "\n-------- inventory0.Updated --------\n";
					res += updateInventoryEvent.ToString(Inventory.Event.Formats.IncludeStacks);
				
					Debug.Log(res);
				};

				inventory0.UpdatedItem += updateItemEvent =>
				{
					var res = "\n-------- inventory0.UpdatedItem --------\n";
					res += updateItemEvent.ToString(ItemStore.Event.Formats.IncludeProperties);
					res += "\n-------- All Items --------\n";
					res += itemStore.ToString(true, true);
				
					Debug.Log(res);
				};
				
				var inventory1 = itemStore.Builder.Inventory();
				
				inventory1.Updated += updateInventoryEvent =>
				{
					var res = "\n-------- inventory1.Updated --------\n";
					res += updateInventoryEvent.ToString(Inventory.Event.Formats.IncludeStacks);
				
					Debug.Log(res);
				};

				inventory1.UpdatedItem += updateItemEvent =>
				{
					var res = "\n-------- inventory1.UpdatedItem --------\n";
					res += updateItemEvent.ToString(ItemStore.Event.Formats.IncludeProperties);
					res += "\n-------- All Items --------\n";
					res += itemStore.ToString(true, true);
				
					Debug.Log(res);
				};

				var filterIntKey = new PropertyKey<int>("some_int_key0");
				
				var stack0 = inventory0.New(
					10,
					out var item0,
					filterIntKey.Pair()
				);

				(stack0 / 2).Transfer(inventory0, inventory1);
				// inventory0.TransferTo()
				
				// inventory0.Increment(stack0);

				// inventory1.Deposit(
				// 	inventory0.Withdrawal(stack0.NewCount(20))
				// );
				//
				// inventory1.Destroy(stack0.NewCount(20));
				//
				// inventory0.Deposit(stack0.NewCount(20));

				Debug.Log("---------");
				Debug.Log(itemStore);
				Debug.Log(inventory0);
				Debug.Log(inventory1);
				Debug.Log("---------");
				itemStore.Cleanup();
			}
		}
		
		#region Events
		void OnAppInstantiated(App app)
		{
			App.S.StateChange += OnAppStateChange;
		}

		void OnAppStateChange(StateChange state)
		{
			if (DebugSettings.SeedOverrideEnabled.Value)
			{
				if (state.Is<GameState>(StateMachine.Events.Begin))
				{
					state.GetPayload<GamePayload>().Game.LevelGeneration.Seed.Value = DebugSettings.SeedOverride.Value;
				}
			}
		}
		
		void OpenSettingsProviderAsset()
		{
			AssetDatabase.OpenAsset(
				AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Editor/" + nameof(DebugSettingsProvider) + ".cs"),
				20
			);
		}
		
		void OnSaveAndCopySerializedGameToClipboard(ModelResult<GameModel> result)
		{
			result.LogIfNotSuccess();
			if (result.IsNotSuccess()) return;
			
			try
			{
				GameStateEditorUtility.GetGame(out var game, out _);
				EditorGUIUtility.systemCopyBuffer = File.ReadAllText(game.Path);
				Debug.Log("Serialized game copied to clipboard");
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
		
		void OnUpdate()
		{
			if (MainMenuStateEditorUtility.GetMainMenu(out var mainMenuModel, out _) && App.S.CurrentEvent == StateMachine.Events.Idle)
			{
				if (!hasMainMenuIdleBeenHandled) OnUpdateMainMenuState(mainMenuModel);
			}
			else if (GameStateEditorUtility.GetGame(out var gameModel, out _) && App.S.CurrentEvent == StateMachine.Events.Idle)
			{
				hasMainMenuIdleBeenHandled = false;
				
				OnUpdateGameState(gameModel);	
			}
		}

		void OnUpdateMainMenuState(MainMenuModel mainMenuModel)
		{
			hasMainMenuIdleBeenHandled = true;
			
			if (onMainMenuIdle == null)
			{
				switch (DebugSettings.MainMenuBehaviour.Value)
				{
					case DebugSettings.MainMenuBehaviours.None:
						break;
					case DebugSettings.MainMenuBehaviours.CreateNewGame:
						Utility.CreateAndStartNewGame(mainMenuModel);
						break;
					case DebugSettings.MainMenuBehaviours.LoadRecentGame:
						Utility.LoadAndStartRecentGame(mainMenuModel);
						break;
					default:
						Debug.LogError("Unrecognized Main Menu Behaviour: "+DebugSettings.MainMenuBehaviour.Value);
						break;
				}
			}
			else
			{
				var callback = onMainMenuIdle;
				onMainMenuIdle = null;
				callback(mainMenuModel);
			}
		}
		
		void OnUpdateGameState(GameModel gameModel)
		{
			if (DebugSettings.AutoRevealRooms.Value && (string.IsNullOrEmpty(lastAutoRevealedRoomsForGameId) || lastAutoRevealedRoomsForGameId != gameModel.Id.Value))
			{
				RevealAllRooms(gameModel);
			}

			if (gameModel.GameResult.Value.State == GameResult.States.Unknown && DebugSettings.GameOverOnDwellerDeath.Value)
			{
				var dwellerCount = gameModel.Dwellers.AllActive.Length;

				if (dwellerCount < previousDwellerCount)
				{
					gameModel.GameResult.Value = new GameResult(
						GameResult.States.Displaying,
						"DEBUG: Dweller died",
						gameModel.SimulationTime.Value
					);

					previousDwellerCount = int.MinValue;
				}
				else previousDwellerCount = dwellerCount;
			}

			if (gameModel.GameResult.Value.State == GameResult.States.Displaying)
			{
				if (DebugSettings.LogGameOverAnalysis.Value && (gameOverAnalyses.None() || gameOverAnalyses.Peek().Id != gameModel.Id.Value))
				{
					var analysis = new GameOverAnalysis();

					analysis.Id = gameModel.Id.Value;
					analysis.Result = gameModel.GameResult.Value;

					analysis.Summary = analysis.Result.Reason;
					analysis.Summary += "\nDweller Deaths:";

					foreach (var logEntry in gameModel.EventLog.Dwellers.PeekAll().Where(e => e.Message.Contains("died")))
					{
						analysis.Summary += $"\n - [ {logEntry.SimulationTime} ] {logEntry.Message}";
					}

					analysis.SurvivalMinimum = analysis.Result.TimeSurvived;
					analysis.SurvivalMaximum = analysis.Result.TimeSurvived;
					analysis.SurvivalAverage = analysis.Result.TimeSurvived;

					var count = 1f;
					
					foreach (var previousAnalysis in gameOverAnalyses)
					{
						if (previousAnalysis.Result.TimeSurvived < analysis.SurvivalMinimum) analysis.SurvivalMinimum = previousAnalysis.Result.TimeSurvived;
						if (analysis.SurvivalMaximum < previousAnalysis.Result.TimeSurvived) analysis.SurvivalMaximum = previousAnalysis.Result.TimeSurvived;
						
						analysis.SurvivalAverage += previousAnalysis.Result.TimeSurvived;
						count++;
					}

					analysis.SurvivalAverage = new DayTime(analysis.SurvivalAverage.TotalTime / count);

					analysis.Summary += "\nSurvival Times:";
					analysis.Summary += "\n - Minimum: \t"+analysis.SurvivalMinimum;
					analysis.Summary += "\n - Maximum: \t"+analysis.SurvivalMaximum;
					analysis.Summary += "\n - Average: \t"+analysis.SurvivalAverage;
					analysis.Summary += "\n";
					analysis.Summary += "\n - Current: \t"+analysis.Result.TimeSurvived;

					analysis.PlaytimeElapsed = gameModel.PlaytimeElapsed.Value;

					analysis.Summary += $"\nPlaytime Elapsed:\n - {analysis.PlaytimeElapsed:g}";
					
					gameOverAnalyses.Push(analysis);
					
					Debug.Log(analysis.Summary);
				}
				
				
				Debug.LogError("re impliment here");
				// if (DebugSettings.AutoNewGame.Value)
				// {
				// 	game.GameResult.Value = game.GameResult.Value.New(GameResult.States.Failure);
				// 	previousDwellerCount = int.MinValue;
				// }
			}
		}

		void TriggerMainMenu(Action<MainMenuModel> onMainMenuIdleCallback)
		{
			onMainMenuIdle = onMainMenuIdleCallback;

			App.S.RequestState<MainMenuPayload>();
		}
		
		void RevealAllRooms(GameModel game)
		{
			if (game.Id.Value == lastAutoRevealedRoomsForGameId) return;
			lastAutoRevealedRoomsForGameId = game.Id.Value;
			foreach (var room in game.Rooms.AllActive) room.IsRevealed.Value = true;
			
			Debug.Log("Revealed rooms for game: "+lastAutoRevealedRoomsForGameId);
			
			var spawnRoom = game.Rooms.AllActive.First(m => m.IsSpawn.Value);
			Debug.DrawLine(
				spawnRoom.Transform.Position.Value,
				spawnRoom.Transform.Position.Value + (Vector3.up * 30f),
				Color.green,
				20f
			);
		}
		#endregion
		
		#region MainMenu Events
		void TriggerMainMenuStartNewGame() => TriggerMainMenu(Utility.CreateAndStartNewGame);
		
		void TriggerMainMenuReloadGame(GameModel gameModel)
		{
			var gameId = gameModel.Id.Value;
			TriggerMainMenu(
				mainMenuModel =>
				{
					App.M.Load<GameModel>(
						gameId,
						loadResult =>
						{
							loadResult.LogIfNotSuccess();
							if (loadResult.Status == ResultStatus.Success)
							{
								mainMenuModel.StartGame(loadResult.TypedModel);
							}
						}
					);
				}
			);
		}
		
		void TriggerMainMenuSaveAndReloadGame(GameModel gameModel)
		{
			App.M.Save(
				gameModel,
				saveResult =>
				{
					saveResult.LogIfNotSuccess();
					if (saveResult.Status == ResultStatus.Success)
					{
						TriggerMainMenuReloadGame(gameModel);
					}
				}
			);
		}
		#endregion

		static class Utility
		{
			public static void CreateAndStartNewGame(MainMenuModel mainMenuModel)
			{
				mainMenuModel.CreateGame(
					createGameResult =>
					{
						createGameResult.LogIfNotSuccess();
						if (createGameResult.Status == ResultStatus.Success)
						{
							App.M.Save(
								createGameResult.Payload,
								saveResult =>
								{
									saveResult.LogIfNotSuccess();
									if (saveResult.Status == ResultStatus.Success)
									{
										App.M.Load<GameModel>(
											saveResult.Model,
											loadResult =>
											{
												loadResult.LogIfNotSuccess();
												if (loadResult.Status == ResultStatus.Success)
												{
													mainMenuModel.StartGame(loadResult.TypedModel);
												}
											}
										);
									}
								}
							);
						}
					}
				);
			}
			
			public static void LoadAndStartRecentGame(MainMenuModel mainMenuModel)
			{
				App.M.Index<GameModel>(
					indexResult =>
					{
						indexResult.LogIfNotSuccess();
						if (indexResult.Status == ResultStatus.Success)
						{
							var mostRecentCompatibleVersion = indexResult.Models
								.OrderByDescending(r => r.Modified.Value)
								.FirstOrDefault(r => r.SupportedVersion.Value);

							if (mostRecentCompatibleVersion == null)
							{
								Debug.LogWarning("Unable to find a compatible save, creating a new game instead");
								CreateAndStartNewGame(mainMenuModel);
							}
							else
							{
								App.M.Load<GameModel>(
									mostRecentCompatibleVersion,
									loadResult =>
									{
										loadResult.LogIfNotSuccess();
										if (loadResult.Status == ResultStatus.Success)
										{
											mainMenuModel.StartGame(loadResult.TypedModel);
										}
									}
								);
							}
						}
					}
				);
			}
		}
	}
}