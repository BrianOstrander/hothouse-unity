using Lunra.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Editor
{
	public static class SceneInspectionSettings
	{
		const string KeyPrefix = SettingsProviderStrings.ProjectKeyPrefix + "SceneInspectionSettings.";

		public static EditorPrefsBool IsInspecting = new EditorPrefsBool(KeyPrefix + "IsInspecting");
		public static EditorPrefsBool IsInspectingBuildings = new EditorPrefsBool(KeyPrefix + "IsInspectingBuildings");
		public static EditorPrefsBool IsInspectingDwellers = new EditorPrefsBool(KeyPrefix + "IsInspectingDwellers");
		public static EditorPrefsBool IsInspectingFlora = new EditorPrefsBool(KeyPrefix + "IsInspectingFlora");
		public static EditorPrefsBool IsInspectingItemDrops = new EditorPrefsBool(KeyPrefix + "IsInspectingItemDrops");
		public static EditorPrefsBool IsInspectingLightLevels = new EditorPrefsBool(KeyPrefix + "IsInspectingLightLevels");
	}
	
	public class SceneInspectionSettingsProvider : SettingsProvider
	{
		static class Content
		{
			public static GUIContent OpenInspectorHandler = new GUIContent("Open Scene Inspection Handler");
		}
		
		public SceneInspectionSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope) { }

		public override void OnGUI(string searchContext)
		{
			if (GUILayout.Button(Content.OpenInspectorHandler)) SceneInspectionHandler.OpenHandlerAsset();
			SceneInspectionSettings.IsInspecting.Draw();
			SceneInspectionSettings.IsInspectingBuildings.Draw();
			SceneInspectionSettings.IsInspectingDwellers.Draw();
			SceneInspectionSettings.IsInspectingFlora.Draw();
			SceneInspectionSettings.IsInspectingItemDrops.Draw();
			SceneInspectionSettings.IsInspectingLightLevels.Draw();
		}

		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider()
		{
			var provider = new SceneInspectionSettingsProvider("Hothouse/Scene Inspection");

			provider.keywords = new[]
			{
				Content.OpenInspectorHandler.text,
				SceneInspectionSettings.IsInspecting.LabelName,
				SceneInspectionSettings.IsInspectingBuildings.LabelName,
				SceneInspectionSettings.IsInspectingDwellers.LabelName,
				SceneInspectionSettings.IsInspectingFlora.LabelName,
				SceneInspectionSettings.IsInspectingItemDrops.LabelName,
				SceneInspectionSettings.IsInspectingLightLevels.LabelName
			};
			
			return provider;
		}
	}
}