using System;
using System.IO;
using System.Linq;
using Lunra.Core;
using Lunra.Editor.Core;
using Lunra.Hothouse.Models;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Models;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Editor
{
	public static class DebugSettings
	{
		const string KeyPrefix = SettingsProviderStrings.ProjectKeyPrefix + "DebugSettings.";

		// public static EditorPrefsBool IsInspecting = new EditorPrefsBool(KeyPrefix + "IsInspecting");
	}
	
	public class DebugSettingsProvider : SettingsProvider
	{
		static class Content
		{
			public static GUIContent OpenDebugSettingsProvider = new GUIContent("Open Debug Settings Provider");
			public static GUIContent OpenSaveLocation = new GUIContent("Open save location");
			public static GUIContent SaveAndCopySerializedGameToClipboard = new GUIContent("Save and copy serialized game to clipboard");
			public static GUIContent QueueNavigationCalculation = new GUIContent("Queue navigation calculation"); 
		}
		
		public DebugSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope) { }
		
		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider()
		{
			var provider = new DebugSettingsProvider("Hothouse/Debug");

			provider.keywords = new []
			{
				Content.OpenDebugSettingsProvider.text,
				Content.OpenSaveLocation.text,
				Content.SaveAndCopySerializedGameToClipboard.text,
				Content.QueueNavigationCalculation.text
			};
			
			return provider;
		}

		public override void OnGUI(string searchContext)
		{
			if (GUILayout.Button(Content.OpenDebugSettingsProvider)) OpenSettingsProviderAsset();
			if (GUILayout.Button(Content.OpenSaveLocation)) EditorUtility.RevealInFinder(Application.persistentDataPath);
			
			GUIExtensions.PushEnabled(
				SettingsProviderCache.GetGame(out var game)	
			);
			{
				if (GUILayout.Button(Content.SaveAndCopySerializedGameToClipboard)) App.M.Save(game, OnSaveAndCopySerializedGameToClipboard);
				if (GUILayout.Button(Content.QueueNavigationCalculation)) game.NavigationMesh.QueueCalculation();
				
				GUILayout.Label("Scratch Area", EditorStyles.boldLabel);

				if (GUILayout.Button("clear first door's obligations"))
				{
					var door = game.Doors.FirstActive();
					var ob = door.Obligations.All.Value.First();
					ob = ob.New(Obligation.States.Blocked).NewId();
					door.Obligations.All.Value = new[] { ob };
				}
			}
			GUIExtensions.PopEnabled();
			
			// if (GUILayout.Button(Content.OpenInspectorHandler)) InspectionHandler.OpenHandlerAsset();
			// InspectionSettings.IsInspecting.Draw();
			// InspectionSettings.IsInspectingBuildings.Draw();
			// InspectionSettings.IsInspectingDwellers.Draw();
			// InspectionSettings.IsInspectingFlora.Draw();
			// InspectionSettings.IsInspectingItemDrops.Draw();
			// InspectionSettings.IsInspectingLightLevels.Draw();
		}
		
		#region Events
		public static void OpenSettingsProviderAsset()
		{
			AssetDatabase.OpenAsset(
				AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Editor/" + nameof(DebugSettingsProvider) + ".cs"),
				20
			);
		}
		
		static void OnSaveAndCopySerializedGameToClipboard(ModelResult<GameModel> result)
		{
			result.LogIfNotSuccess();
			if (result.IsNotSuccess()) return;
			
			try
			{
				SettingsProviderCache.GetGame(out var game);
				EditorGUIUtility.systemCopyBuffer = File.ReadAllText(game.Path);
				Debug.Log("Serialized game copied to clipboard");
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
		#endregion
	}
}