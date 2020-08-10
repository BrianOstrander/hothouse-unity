using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lunra.Core;
using Lunra.Editor.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Services;
using Lunra.Hothouse.Services.Editor;
using Lunra.NumberDemon;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Models;
using Lunra.StyxMvp.Services;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Editor
{
	public static class DebugSettings
	{
		const string KeyPrefix = SettingsProviderStrings.ProjectKeyPrefix + "DebugSettings.";

		public static EditorPrefsBool AutoRevealRooms { get; } = new EditorPrefsBool(KeyPrefix + "AutoRevealRooms");
		public static EditorPrefsBool LogGameOverAnalysis { get; } = new EditorPrefsBool(KeyPrefix + "LogGameOverAnalysis");
		public static EditorPrefsBool GameOverOnDwellerDeath { get; } = new EditorPrefsBool(KeyPrefix + "GameOverOnDwellerDeath");
		public static EditorPrefsBool AutoRestartOnGameOver { get; } = new EditorPrefsBool(KeyPrefix + "AutoRestartOnGameOver");
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
			public static GUIContent SaveAndLoadGame = new GUIContent("Save & Load Game");
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
				DebugSettings.AutoRestartOnGameOver.LabelName,
				DebugSettings.SeedOverrideEnabled.LabelName,
				DebugSettings.SeedOverride.LabelName,
				
				Content.OpenDebugSettingsProvider.text,
				Content.OpenSaveLocation.text,
				Content.SaveAndCopySerializedGameToClipboard.text,
				Content.StartNewGame.text,
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

			var isInGame = GameStateEditorUtility.GetGame(out var game, out var state);
			
			GUIExtensions.PushEnabled(isInGame);
			{
				if (GUILayout.Button(Content.SaveAndCopySerializedGameToClipboard)) App.M.Save(game, OnSaveAndCopySerializedGameToClipboard);
				if (GUILayout.Button(Content.QueueNavigationCalculation)) game.NavigationMesh.QueueCalculation();
				
				GUILayout.BeginHorizontal();
				{
					if (GUILayout.Button(Content.StartNewGame))
					{
						App.S.RequestState(
							new MainMenuPayload
							{
								Preferences = state.Payload.Preferences
							}
						);
					}
					
					if (GUILayout.Button(Content.SaveAndLoadGame))
					{
						App.M.Save(
							game,
							result =>
							{
								result.LogIfNotSuccess();
								if (result.Status != ResultStatus.Success) return;
								
								App.S.RequestState(
									new MainMenuPayload
									{
										AutoLoadGame = game.Id.Value,
										Preferences = state.Payload.Preferences
									}
								);
							}
						);
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					GUIExtensions.PushEnabled(true, true);
					{
						DebugSettings.AutoRevealRooms.Draw(GUILayout.ExpandWidth(false));
					}
					GUIExtensions.PopEnabled();

					if (GUILayout.Button(Content.RevealAllRooms)) RevealAllRooms(game);

					if (GUILayout.Button(Content.OpenAllDoors))
					{
						foreach (var room in game.Rooms.AllActive) room.IsRevealed.Value = true;
						foreach (var door in game.Doors.AllActive) door.IsOpen.Value = true;
					}
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
						EditorGUILayout.IntField(isInGame ? game.LevelGeneration.Seed.Value : 0);
					}
					GUIExtensions.PopEnabled();
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				{
					GUILayout.Label("Simulation Speed", GUILayout.ExpandWidth(false));
					GUILayout.Label($"{(isInGame ? game.SimulationMultiplier.Value : 0f):N0}x", GUILayout.Width(32f));
					if (GUILayout.Button(Content.SimulationSpeedReset)) game.SimulationMultiplier.Value = 1f;
					if (GUILayout.Button(Content.SimulationSpeedDecrease)) game.SimulationMultiplier.Value = Mathf.Max(0f, game.SimulationMultiplier.Value - 1f);
					if (GUILayout.Button(Content.SimulationSpeedIncrease)) game.SimulationMultiplier.Value++;
				}
				GUILayout.EndHorizontal();
				
				DebugSettings.LogGameOverAnalysis.Draw();
				DebugSettings.AutoRestartOnGameOver.Draw();
				DebugSettings.GameOverOnDwellerDeath.Draw();
			}
			GUIExtensions.PopEnabled();
			
			if (GUILayout.Button(Content.ValidateSerializedProperties))
			{
				SerializedPropertiesUtility.Validate();
			}
			
			GUILayout.Label("Scratch Area", EditorStyles.boldLabel);
			
			if (GUILayout.Button("Test surface point gen"))
			{
				var gen = new Demon();
				for (var i = 0; i < 1000; i++)
				{
					var pos = gen.NextNormal;
						
					Debug.DrawLine(
						pos + (Vector3.up * 6f),
						(pos + (Vector3.up * 6f)) + (pos * 0.1f),
						Color.red,
						10f
					);
				}
			}
			
			if (GUILayout.Button("Test inventory component"))
			{
				var testInventory = new InventoryComponent();
				
				testInventory.Reset(
					InventoryPermission.AllForAnyJob(), 
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Scrap, 10 }
							}
						)	
					)
				);
				
				Debug.Log(testInventory);

				testInventory.Add((Inventory.Types.Scrap, 5).ToInventory());
				
				Debug.Log("Added 5 Scrap - " + testInventory);
				
				// testInventory.Add((Inventory.Types.Scrap, 5).ToInventory());
				//
				// Debug.Log("Added 5 Scrap - " + testInventory);
				
				testInventory.AddForbidden((Inventory.Types.Scrap, 5).ToInventory());
				
				Debug.Log("AddForbidden 5 Scrap - " + testInventory);
				
				testInventory.AddReserved((Inventory.Types.Scrap, 5).ToInventory());
				
				Debug.Log("AddReserved 5 Scrap - " + testInventory);

				testInventory.RemoveForbidden((Inventory.Types.Scrap, 5).ToInventory());
				
				Debug.Log("RemoveForbidden 5 Scrap - " + testInventory);
				
				testInventory.RemoveReserved((Inventory.Types.Scrap, 5).ToInventory());
				
				Debug.Log("RemoveReserved 5 Scrap - " + testInventory);

				// var gen = new Demon();
				// for (var i = 0; i < 10; i++)
				// {
				// 	
				// 	
				// }
				
				Debug.Log("------");
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
			if (GameStateEditorUtility.GetGame(out var game, out _) && App.S.CurrentEvent == StateMachine.Events.Idle)
			{
				if (DebugSettings.AutoRevealRooms.Value && (string.IsNullOrEmpty(lastAutoRevealedRoomsForGameId) || lastAutoRevealedRoomsForGameId != game.Id.Value))
				{
					RevealAllRooms(game);
				}

				if (game.GameResult.Value.State == GameResult.States.Unknown && DebugSettings.GameOverOnDwellerDeath.Value)
				{
					var dwellerCount = game.Dwellers.AllActive.Length;

					if (dwellerCount < previousDwellerCount)
					{
						game.GameResult.Value = new GameResult(
							GameResult.States.Displaying,
							"DEBUG: Dweller died",
							game.SimulationTime.Value
						);

						previousDwellerCount = int.MinValue;
					}
					else previousDwellerCount = dwellerCount;
				}

				if (game.GameResult.Value.State == GameResult.States.Displaying)
				{
					if (DebugSettings.LogGameOverAnalysis.Value && (gameOverAnalyses.None() || gameOverAnalyses.Peek().Id != game.Id.Value))
					{
						var analysis = new GameOverAnalysis();

						analysis.Result = game.GameResult.Value;

						analysis.Summary = analysis.Result.Reason;
						analysis.Summary += "\nDweller Deaths:";

						foreach (var logEntry in game.EventLog.Dwellers.PeekAll().Where(e => e.Message.Contains("died")))
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

						analysis.PlaytimeElapsed = game.PlaytimeElapsed.Value;

						analysis.Summary += $"\nPlaytime Elapsed:\n - {analysis.PlaytimeElapsed:g}";
						
						gameOverAnalyses.Push(analysis);
						
						Debug.Log(analysis.Summary);
					}
					
					if (DebugSettings.AutoRestartOnGameOver.Value)
					{
						game.GameResult.Value = game.GameResult.Value.New(GameResult.States.Failure);
						previousDwellerCount = int.MinValue;
					}
				}
			}
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
	}
}