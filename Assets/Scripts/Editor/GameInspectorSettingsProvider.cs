using System.IO;
using Lunra.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lunra.Hothouse.Editor
{
	public static class GameInspectionSettings
	{
		const string KeyPrefix = "Lunra.Hothouse.GameInspectionSettings.";

		public static EditorPrefsBool IsInspecting = new EditorPrefsBool(KeyPrefix + "IsInspecting");
		public static EditorPrefsBool IsInspectingBuildings = new EditorPrefsBool(KeyPrefix + "IsInspectingBuildings");
		public static EditorPrefsBool IsInspectingDwellers = new EditorPrefsBool(KeyPrefix + "IsInspectingDwellers");
		public static EditorPrefsBool IsInspectingFlora = new EditorPrefsBool(KeyPrefix + "IsInspectingFlora");
	}
	
	public class GameInspectorSettingsProvider : SettingsProvider
	{
		static class Content
		{
			public static GUIContent OpenInspectorHandler = new GUIContent("Open Inspector Handler");
		}
		
		public GameInspectorSettingsProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope) { }

		public override void OnGUI(string searchContext)
		{
			if (GUILayout.Button(Content.OpenInspectorHandler)) GameInspectorHandler.OpenHandlerAsset();
			GameInspectionSettings.IsInspecting.Draw();
			GameInspectionSettings.IsInspectingBuildings.Draw();
			GameInspectionSettings.IsInspectingDwellers.Draw();
			GameInspectionSettings.IsInspectingFlora.Draw();
		}

		[SettingsProvider]
		public static SettingsProvider CreateMyCustomSettingsProvider()
		{
			var provider = new GameInspectorSettingsProvider("Wild Vacuum/Game Inspector", SettingsScope.Project);

			provider.keywords = new[]
			{
				Content.OpenInspectorHandler.text,
				GameInspectionSettings.IsInspecting.LabelName,
				GameInspectionSettings.IsInspectingBuildings.LabelName,
				GameInspectionSettings.IsInspectingDwellers.LabelName,
				GameInspectionSettings.IsInspectingFlora.LabelName
			};
			
			return provider;
		}
	}
}