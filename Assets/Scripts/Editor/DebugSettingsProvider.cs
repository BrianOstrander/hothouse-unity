using System;
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

		public static EditorPrefsBool AutoRevealRooms = new EditorPrefsBool(KeyPrefix + "AutoRevealRooms");
	}
	
	public class DebugSettingsProvider : SettingsProvider
	{
		static class Content
		{
			public static GUIContent OpenDebugSettingsProvider = new GUIContent("Open Debug Settings Provider");
			public static GUIContent OpenSaveLocation = new GUIContent("Open save location");
			public static GUIContent SaveAndCopySerializedGameToClipboard = new GUIContent("Save and copy serialized game to clipboard");
			public static GUIContent StartNewGame = new GUIContent("Start New Game");
			public static GUIContent QueueNavigationCalculation = new GUIContent("Queue navigation calculation");
			public static GUIContent RevealAllRooms = new GUIContent("Reveal All Rooms");
			public static GUIContent AutoRevealRooms = new GUIContent("Auto Reveal");
			public static GUIContent OpenAllDoors = new GUIContent("Open All Doors");
			public static GUIContent SimulationSpeedIncrease = new GUIContent("->", "Increase");
			public static GUIContent SimulationSpeedDecrease = new GUIContent("<-", "Decrease");
		}

		string lastAutoRevealedRoomsForGameId;

		public DebugSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope)
		{
			EditorApplication.update -= OnUpdate;
			EditorApplication.update += OnUpdate;
		}
		
		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider()
		{
			var provider = new DebugSettingsProvider("Hothouse/Debug");

			provider.keywords = new []
			{
				Content.OpenDebugSettingsProvider.text,
				Content.OpenSaveLocation.text,
				Content.SaveAndCopySerializedGameToClipboard.text,
				Content.StartNewGame.text,
				Content.QueueNavigationCalculation.text,
				Content.RevealAllRooms.text,
				Content.AutoRevealRooms.text,
				Content.OpenAllDoors.text,
				Content.SimulationSpeedIncrease.text,
				Content.SimulationSpeedDecrease.text
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
				if (GUILayout.Button(Content.StartNewGame))
				{
					App.S.RequestState(
						new MainMenuPayload
						{
							Preferences = state.Payload.Preferences
						}
					);
				}
				
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
					GUILayout.Label("Simulation Speed", GUILayout.ExpandWidth(false));
					if (GUILayout.Button(Content.SimulationSpeedDecrease)) game.SimulationMultiplier.Value = Mathf.Max(0f, game.SimulationMultiplier.Value - 1f);
					if (GUILayout.Button(Content.SimulationSpeedIncrease)) game.SimulationMultiplier.Value++;
				}
				GUILayout.EndHorizontal();
				
				GUILayout.Label("Scratch Area", EditorStyles.boldLabel);

				if (GUILayout.Button("kill any dwellers with a bed"))
				{
					foreach (var dweller in game.Dwellers.AllActive.Where(m => !string.IsNullOrEmpty(m.Bed.Value.Id)))
					{
						Damage.ApplyGeneric(1000f, dweller);
					}
				}
				
				if (GUILayout.Button("reset promiseId of first door's obligations"))
				{
					var door = game.Doors.FirstActive();
					var ob = door.Obligations.All.Value.First();
					ob = ob.New(Obligation.States.Blocked).NewPromiseId();
					door.Obligations.All.Value = new[] { ob };
				}
				
				if (GUILayout.Button("clear first door's obligations"))
				{
					game.Doors.FirstActive().Obligations.All.Value = new Obligation[0];
				}
				
				if (GUILayout.Button("first door rando obl"))
				{
					game.ObligationIndicators.Register(
						Obligation.New(
							ObligationCategories.Door.Open,
							0,
							ObligationCategories.GetJobs(),
							Obligation.ConcentrationRequirements.Instant,
							Interval.Zero()
						),
						game.Doors.FirstActive()
					);
				}
			}
			GUIExtensions.PopEnabled();
			
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
		}
		
		#region Events
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
			if (GameStateEditorUtility.GetGame(out var game, out var state) && DebugSettings.AutoRevealRooms.Value && App.S.CurrentEvent == StateMachine.Events.Idle)
			{
				if (string.IsNullOrEmpty(lastAutoRevealedRoomsForGameId) || lastAutoRevealedRoomsForGameId != game.Id.Value)
				{
					RevealAllRooms(game);
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