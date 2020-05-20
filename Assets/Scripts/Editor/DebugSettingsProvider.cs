using System;
using System.IO;
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
			public static GUIContent OpenSaveLocation = new GUIContent("Open save location");
			public static GUIContent SaveAndCopySerializedGameToClipboard = new GUIContent("Save and copy serialized game to clipboard");
			public static GUIContent QueueNavigationCalculation = new GUIContent("Queue navigation calculation"); 
		}
		
		public DebugSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope) { }
		
		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider()
		{
			var provider = new DebugSettingsProvider("Hothouse/Debug");

			provider.keywords = new string[]
			{
				Content.OpenSaveLocation.text,
				Content.SaveAndCopySerializedGameToClipboard.text
			};
			
			return provider;
		}

		public override void OnGUI(string searchContext)
		{
			if (GUILayout.Button(Content.OpenSaveLocation)) EditorUtility.RevealInFinder(Application.persistentDataPath);
			
			GUIExtensions.PushEnabled(
				SettingsProviderCache.GetGame(out var game)	
			);

			if (GUILayout.Button(Content.SaveAndCopySerializedGameToClipboard)) App.M.Save(game, OnSaveAndCopySerializedGameToClipboard);
			if (GUILayout.Button(Content.QueueNavigationCalculation)) game.NavigationMesh.QueueCalculation();

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