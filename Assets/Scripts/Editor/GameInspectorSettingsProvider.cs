using System.IO;
using Lunra.Editor.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lunra.WildVacuum.Editor
{
	public static class GameInspectionSettings
	{
		const string KeyPrefix = "Lunra.WildVacuum.GameInspectionSettings.";

		public static EditorPrefsBool IsInspecting = new EditorPrefsBool(KeyPrefix + "IsInspecting");
		public static EditorPrefsBool IsInspectingBuildings = new EditorPrefsBool(KeyPrefix + "IsInspectingBuildings");
		public static EditorPrefsBool IsInspectingDwellers = new EditorPrefsBool(KeyPrefix + "IsInspectingDwellers");
	}
	
	public class GameInspectorSettingsProvider : SettingsProvider
	{
		public GameInspectorSettingsProvider(string path, SettingsScope scope = SettingsScope.User) : base(path, scope) { }

		public override void OnGUI(string searchContext)
		{
			GameInspectionSettings.IsInspecting.Draw();
			GameInspectionSettings.IsInspectingBuildings.Draw();
			GameInspectionSettings.IsInspectingDwellers.Draw();
		}

		[SettingsProvider]
		public static SettingsProvider CreateMyCustomSettingsProvider()
		{
			var provider = new GameInspectorSettingsProvider("Wild Vacuum/Game Inspector", SettingsScope.Project);

			provider.keywords = new[]
			{
				GameInspectionSettings.IsInspecting.LabelName,
				GameInspectionSettings.IsInspectingBuildings.LabelName,
				GameInspectionSettings.IsInspectingDwellers.LabelName
			};
			
			return provider;
		}
	}
}