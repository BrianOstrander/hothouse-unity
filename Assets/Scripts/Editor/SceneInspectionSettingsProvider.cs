using System.Linq;
using Lunra.Core;
using Lunra.Editor.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Services.Editor;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Editor
{
	public static class SceneInspectionSettings
	{
		const string KeyPrefix = SettingsProviderStrings.ProjectKeyPrefix + "SceneInspectionSettings.";

		public static EditorPrefsBool IsInspecting = new EditorPrefsBool(KeyPrefix + "IsInspecting");
		public static EditorPrefsBool IsInspectingBuildings = new EditorPrefsBool(KeyPrefix + "IsInspectingBuildings");
		public static EditorPrefsBool IsInspectingEntrances = new EditorPrefsBool(KeyPrefix + "IsInspectingEntrances");
		public static EditorPrefsBool IsInspectingDwellers = new EditorPrefsBool(KeyPrefix + "IsInspectingDwellers");
		public static EditorPrefsBool IsInspectingOtherAgents = new EditorPrefsBool(KeyPrefix + "IsInspectingOtherAgents");
		public static EditorPrefsBool IsInspectingGenerators = new EditorPrefsBool(KeyPrefix + "IsInspectingGenerators");
		public static EditorPrefsBool IsInspectingFlora = new EditorPrefsBool(KeyPrefix + "IsInspectingFlora");
		public static EditorPrefsBool IsInspectingDebris = new EditorPrefsBool(KeyPrefix + "IsInspectingDebris");
		public static EditorPrefsBool IsInspectingItemDrops = new EditorPrefsBool(KeyPrefix + "IsInspectingItemDrops");
		public static EditorPrefsBool IsInspectingLightLevels = new EditorPrefsBool(KeyPrefix + "IsInspectingLightLevels");
		public static EditorPrefsBool IsInspectingObligations = new EditorPrefsBool(KeyPrefix + "IsInspectingObligations");
		public static EditorPrefsBool IsInspectingRooms = new EditorPrefsBool(KeyPrefix + "IsInspectingRooms");
		public static EditorPrefsBool IsInspectingDoors = new EditorPrefsBool(KeyPrefix + "IsInspectingDoors");
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
			
			GUIExtensions.PushEnabled(SceneInspectionSettings.IsInspecting.Value);
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.BeginVertical();
					{
						SceneInspectionSettings.IsInspectingBuildings.Draw();
						SceneInspectionSettings.IsInspectingDwellers.Draw();
						SceneInspectionSettings.IsInspectingOtherAgents.Draw();
						SceneInspectionSettings.IsInspectingGenerators.Draw();
						SceneInspectionSettings.IsInspectingFlora.Draw();
						SceneInspectionSettings.IsInspectingDebris.Draw();
						SceneInspectionSettings.IsInspectingItemDrops.Draw();
						SceneInspectionSettings.IsInspectingRooms.Draw();
						SceneInspectionSettings.IsInspectingDoors.Draw();
					}
					GUILayout.EndVertical();

					GUILayout.BeginVertical();
					{
						SceneInspectionSettings.IsInspectingEntrances.Draw();
						SceneInspectionSettings.IsInspectingLightLevels.Draw();
						SceneInspectionSettings.IsInspectingObligations.Draw();
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();
				
				OnDwellersGui();
			}
			GUIExtensions.PopEnabled();

		}

		void OnDwellersGui()
		{
			if (!SceneInspectionSettings.IsInspecting.Value) return;
			if (!SceneInspectionSettings.IsInspectingDwellers.Value) return;
			if (!GameStateEditorUtility.GetGameState(out var gameState)) return;
			
			EditorGUIExtensions.PushIndent();
			{
				foreach (var dweller in gameState.Payload.Game.Dwellers.AllActive)
				{
					GUILayoutExtensions.BeginVertical(EditorStyles.helpBox, Color.white);
					{
						GUILayout.BeginHorizontal();
						{
							GUILayout.Label("Dweller [ " + dweller.ShortId + " ] : " + dweller.Name.Value + " - " + dweller.Job.Value, EditorStyles.boldLabel);
							
							if (GUILayout.Button("Satisfy", GUILayout.ExpandWidth(false)))
							{
								dweller.Goals.Apply(
									EnumExtensions.GetValues(Motives.Unknown, Motives.None)
										.Select(m => (m, -1f))
										.ToArray()
								);
							}
							
							if (GUILayout.Button("Hurt", GUILayout.ExpandWidth(false)))
							{
								Damage.ApplyGeneric(
									dweller.Health.Current.Value * 0.25f,
									dweller
								);
							}
							if (GUILayout.Button("Kill", GUILayout.ExpandWidth(false)))
							{
								Damage.ApplyGeneric(
									9999f,
									dweller
								);
							}
						}
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();
						{
							if (GUILayout.Button("Break Inventory Promises")) dweller.InventoryPromises.BreakAll();
						}
						GUILayout.EndHorizontal();
						
						var addRemoveButtonWidth = GUILayout.Width(48f);
						
						GUILayout.BeginHorizontal();
						{
							GUILayout.Label("Add");
							
							foreach (var motive in EnumExtensions.GetValues(Motives.Unknown, Motives.None))
							{
								if (GUILayout.Button(motive.ToString(), addRemoveButtonWidth))
								{
									dweller.Goals.Apply((motive, 0.5f));
								}	
							}
						}
						GUILayout.EndHorizontal();
						
						GUILayout.BeginHorizontal();
						{
							GUILayout.Label("Remove");
							
							foreach (var motive in EnumExtensions.GetValues(Motives.Unknown, Motives.None))
							{
								if (GUILayout.Button(motive.ToString(), addRemoveButtonWidth))
								{
									dweller.Goals.Apply((motive, -0.5f));
								}	
							}
						}
						GUILayout.EndHorizontal();
						
						dweller.IsDebugging = EditorGUILayout.Toggle(nameof(dweller.IsDebugging), dweller.IsDebugging);

					}
					GUILayoutExtensions.EndVertical();
				}
			}
			EditorGUIExtensions.PopIndent();
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
				SceneInspectionSettings.IsInspectingEntrances.LabelName,
				SceneInspectionSettings.IsInspectingDwellers.LabelName,
				SceneInspectionSettings.IsInspectingOtherAgents.LabelName,
				SceneInspectionSettings.IsInspectingGenerators.LabelName,
				SceneInspectionSettings.IsInspectingFlora.LabelName,
				SceneInspectionSettings.IsInspectingDebris.LabelName,
				SceneInspectionSettings.IsInspectingItemDrops.LabelName,
				SceneInspectionSettings.IsInspectingLightLevels.LabelName,
				SceneInspectionSettings.IsInspectingObligations.LabelName,
				SceneInspectionSettings.IsInspectingRooms.LabelName,
				SceneInspectionSettings.IsInspectingDoors.LabelName
			};
			
			return provider;
		}
	}
}