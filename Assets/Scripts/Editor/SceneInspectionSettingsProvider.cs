using System;
using System.Linq;
using Lunra.Core;
using Lunra.Editor.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Services.Editor;
using Lunra.StyxMvp.Models;
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
				
				OnInventoryGui();
			}
			GUIExtensions.PopEnabled();

		}

		void OnDwellersGui()
		{
			DrawModelGui<DwellerModel>(
				null,
				m => "Dweller",
				m => $"{m.Name.Value} - {m.Job.Value}",
				m =>
				{
					if (GUILayout.Button("Satisfy", GUILayout.ExpandWidth(false)))
					{
						m.Goals.Apply(
							EnumExtensions.GetValues(Motives.Unknown, Motives.None)
								.Select(motive => (motive, -1f))
								.ToArray()
						);
					}
							
					if (GUILayout.Button("Hurt", GUILayout.ExpandWidth(false)))
					{
						Damage.ApplyGeneric(
							m.Health.Current.Value * 0.25f,
							m
						);
					}
					if (GUILayout.Button("Kill", GUILayout.ExpandWidth(false)))
					{
						Damage.ApplyGeneric(
							9999f,
							m
						);
					}
				},
				m =>
				{
					GUILayout.BeginHorizontal();
					{
						if (GUILayout.Button("Break Inventory Promises")) m.InventoryPromises.BreakAll();
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
								m.Goals.Apply((motive, 0.5f));
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
								m.Goals.Apply((motive, -0.5f));
							}	
						}
					}
					GUILayout.EndHorizontal();
						
					m.IsDebugging = EditorGUILayout.Toggle(nameof(m.IsDebugging), m.IsDebugging);
				}
			);
		}
		
		void OnInventoryGui()
		{
			DrawModelGui<IInventoryModel>(
				m =>
				{
					switch (m)
					{
						case FloraModel _:
							return SceneInspectionSettings.IsInspectingFlora.Value;
						case BuildingModel _:
							return SceneInspectionSettings.IsInspectingBuildings.Value;
						case ItemDropModel _:
							return SceneInspectionSettings.IsInspectingItemDrops.Value;
						case DwellerModel _:
							return SceneInspectionSettings.IsInspectingDwellers.Value;
						case DebrisModel _:
							return SceneInspectionSettings.IsInspectingDebris.Value;
						default:
							return false;
					}
				},
				m => $"Inventory.{m.GetType().Name}",
				drawBody: m =>
				{
					foreach (var (capacityPool, _) in m.Inventory.Container.All(i => i[Items.Keys.Shared.Type] == Items.Values.Shared.Types.CapacityPool).ToArray())
					{
						GUILayout.BeginHorizontal();
						{
							GUILayout.Label($"Pool [ {capacityPool.Id} ]", GUILayout.Width(64f));

							if (GUILayout.Button("None", EditorStyles.miniButtonLeft))
							{
								m.Inventory.SetForbidden(capacityPool.Id, false);
								m.Inventory.SetCapacity(capacityPool.Id, capacityPool[Items.Keys.CapacityPool.CountMaximum]);
							}
							if (GUILayout.Button("Zero", EditorStyles.miniButtonMid))
							{
								m.Inventory.SetForbidden(capacityPool.Id, false);
								m.Inventory.SetCapacity(capacityPool.Id, 0);
							}
							if (GUILayout.Button("One", EditorStyles.miniButtonMid))
							{
								m.Inventory.SetForbidden(capacityPool.Id, false);
								m.Inventory.SetCapacity(capacityPool.Id, 1);
							}
							if (GUILayout.Button("Unlimited", EditorStyles.miniButtonRight))
							{
								m.Inventory.SetForbidden(capacityPool.Id, false);
								m.Inventory.SetCapacity(capacityPool.Id, int.MaxValue);
							}
							if (GUILayout.Button("Forbidden", EditorStyles.miniButton))
							{
								m.Inventory.SetForbidden(capacityPool.Id, true);
							}
						}
						GUILayout.EndHorizontal();
						
						foreach (var (capacity, _) in m.Inventory.Container.All(i => i[Items.Keys.Capacity.Pool] == capacityPool.Id).ToArray())
						{
							GUILayout.BeginHorizontal();
							{
								GUILayout.Space(16f);
								GUILayout.Label($"\tCapacity [ {capacity.Id} ]");

								if (GUILayout.Button("None", EditorStyles.miniButtonLeft))
								{
									m.Inventory.SetCapacity(capacity.Id, capacity[Items.Keys.Capacity.CountMaximum]);
								}
								if (GUILayout.Button("Zero", EditorStyles.miniButtonMid))
								{
									m.Inventory.SetCapacity(capacity.Id, 0);
								}
								if (GUILayout.Button("One", EditorStyles.miniButtonMid))
								{
									m.Inventory.SetCapacity(capacity.Id, 1);
								}
								if (GUILayout.Button("Unlimited", EditorStyles.miniButtonRight))
								{
									m.Inventory.SetCapacity(capacity.Id, int.MaxValue);
								}
							}
							GUILayout.EndHorizontal();
							
						}
					}
				}
			);
		}
		
		void DrawModelGui<M>(
			Func<M, bool> predicate,
			Func<M, string> getHeaderPrefix,
			Func<M, string> getHeaderSuffix = null,
			Action<M> drawHeaderRight = null,
			Action<M> drawBody = null
		)
			where M : IModel
		{
			if (!SceneInspectionSettings.IsInspecting.Value) return;
			if (!GameStateEditorUtility.GetGameState(out var gameState)) return;
			
			EditorGUIExtensions.PushIndent();
			{
				foreach (var model in gameState.Payload.Game.Query.All(predicate))
				{
					GUILayoutExtensions.BeginVertical(EditorStyles.helpBox, Color.white);
					{
						GUILayout.BeginHorizontal();
						{
							var header = $"{getHeaderPrefix(model)} [ {model.ShortId} ]";
							if (getHeaderSuffix != null) header += $" : {getHeaderSuffix(model)}";
							GUILayout.Label(header, EditorStyles.boldLabel);

							drawHeaderRight?.Invoke(model);
						}
						GUILayout.EndHorizontal();

						drawBody?.Invoke(model);
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