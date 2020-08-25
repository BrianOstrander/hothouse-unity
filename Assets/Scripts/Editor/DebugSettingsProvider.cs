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

			if (GUILayout.Button("Test Satchel 0f"))
			{
				var itemStore = new ItemStore();
				itemStore.Initialize();

				// itemStore.Updated += updateEvent =>
				// {
				// 	var res = updateEvent.ToString(ItemStore.Event.Formats.IncludeProperties);
				// 	res += "\n-------- All Items --------\n";
				// 	res += itemStore.ToString(true, true);
				//
				// 	Debug.Log(res);
				// };
				
				void applyConstraint(
					ItemConstraint constraint,
					params ItemStack[] stacks
				)
				{
					var res = stacks.Aggregate("Original:", (r, e) => $"{r}\n\t{e}");
					
					var anyOverflow = constraint.Apply(
						stacks,
						out var result,
						out var overflow
					);

					res += result.Aggregate("\nResult:", (r, e) => $"{r}\n\t{e}");

					if (anyOverflow)
					{
						res = "[ Overflow Occured ]\n" + res;
						res += overflow.Aggregate("\nOverflow:", (r, e) => $"{r}\n\t{e}");
					}
					else res = "[ No Overflow ]\n" + res;

					Debug.Log(res);
				}

				var filterIntKey = "some_int_key0";

				var item0 = itemStore.New(
					(filterIntKey, 0)
				);
				
				var item1 = itemStore.New(
					(filterIntKey, 1)
				);

				applyConstraint(
					ItemConstraintBuilder
						.Begin(itemStore)
						.WithLimitOf(10)
						.WithLimitDefaultOf(5)
						.Permit(
							ItemFilterBuilder
								.Begin(itemStore)
								.RequireAll(
									PropertyValidation.Default.Int.EqualTo(filterIntKey, 0)
								)
						),
					item0.NewStack(10),
					item1.NewStack(10)
				);
			}
			
			if (GUILayout.Button("Test Satchel 0e"))
			{
				var itemStore = new ItemStore();
				itemStore.Initialize();

				itemStore.Updated += updateEvent =>
				{
					var res = updateEvent.ToString(ItemStore.Event.Formats.IncludeProperties);
					res += "\n-------- All Items --------\n";
					res += itemStore.ToString(true, true);

					Debug.Log(res);
				};

				var itemInventory0 = new ItemInventory().Initialize(itemStore);
				var itemInventory1 = new ItemInventory().Initialize(itemStore);

				itemInventory0.Updated += itemInventoryEvent =>
				{
					var res = $"Inventory0 Update: {itemInventoryEvent.ToString(ItemInventory.Event.Formats.IncludeStacks)}\n{itemInventory0}";
					Debug.Log(res);
				};

				itemInventory1.Updated += itemInventoryEvent =>
				{
					var res = $"Inventory1 Update: {itemInventoryEvent.ToString(ItemInventory.Event.Formats.IncludeStacks)}\n{itemInventory1}";
					Debug.Log(res);
				};

				var filterIntKey = "some_int_key0";

				var item0 = itemStore.New(
					(filterIntKey, 10)
				);
				
				var item1 = itemStore.New(
					(filterIntKey, 5)
				);

				void modify(
					ItemInventory targetInventory,
					params (Item Item, int Count)[] items
				)
				{
					var didModify = targetInventory.Modify(
						items,
						out var clampResult,
						out var triggerUpdate
					);

					if (didModify)
					{
						if (clampResult.Any())
						{
							Debug.Log(clampResult.Aggregate("ClampedModify", (r, e) => $"{r}\n{e.Item.ToString(e.Count)}"));
						}
						triggerUpdate?.Invoke(DateTime.Now);
					}

					Debug.Log("modify result:\n" + targetInventory);
				}
				
				void updateConstraint(
					ItemInventory targetInventory,
					ItemConstraint constraint
				)
				{
					var didClampingOccur = targetInventory.UpdateConstraint(
						constraint,
						out var clampResult
					);

					if (didClampingOccur)
					{
						if (clampResult.Any())
						{
							Debug.Log(clampResult.Aggregate("ClampedUpdateConstraint", (r, e) => $"{r}\n{e}"));
						}
					}

					Debug.Log("updateConstraint result:\n" + targetInventory);
				}
				
				modify(
					itemInventory0,
					(item0, 20),
					(item1, 20)
				);
				
				updateConstraint(
					itemInventory0,
					ItemConstraint.ByCount(50)
				);
				
				updateConstraint(
					itemInventory0,
					ItemConstraint.ByFilter(
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAny(
								PropertyValidation.Default.Int.GreaterThanOrEqualTo(filterIntKey, int.MinValue)	
							)
					)
				);
				
				updateConstraint(
					itemInventory0,
					ItemConstraint.ByFilter(
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAny(
								PropertyValidation.Default.Int.GreaterThanOrEqualTo(filterIntKey, 6)	
							)
					)
				);
			}

			if (GUILayout.Button("Test Satchel 0d"))
			{
				var itemStore = new ItemStore();
				itemStore.Initialize();

				itemStore.Updated += updateEvent =>
				{
					var res = updateEvent.ToString(ItemStore.Event.Formats.IncludeProperties);
					res += "\n-------- All Items --------\n";
					res += itemStore.ToString(true, true);

					Debug.Log(res);
				};

				var itemInventory0 = new ItemInventory().Initialize(itemStore);
				var itemInventory1 = new ItemInventory().Initialize(itemStore);

				itemInventory0.Updated += itemInventoryEvent =>
				{
					var res = $"Inventory0 Update: {itemInventoryEvent.ToString(ItemInventory.Event.Formats.IncludeStacks)}\n{itemInventory0}";
					Debug.Log(res);
				};

				itemInventory1.Updated += itemInventoryEvent =>
				{
					var res = $"Inventory1 Update: {itemInventoryEvent.ToString(ItemInventory.Event.Formats.IncludeStacks)}\n{itemInventory1}";
					Debug.Log(res);
				};
				
				var filterBoolValue = true;
				var filterBoolKey = "some_bool_key0";
				
				var filterIntValue = 10;
				var filterIntKey = "some_int_key0";
				
				var filterFloatValue = 10f;
				var filterFloatKey = "some_float_key0";
				
				var filterStringValue = "ro";
				var filterStringKey = "some_string_key0";
				
				var item0 = itemStore.New(
					(filterBoolKey, true),
					(filterIntKey, 10),
					(filterFloatKey, 10f),
					(filterStringKey, "rofl")
				);

				var filters = new Dictionary<string, ItemFilter>
				{
					{
						"0 Should be true",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.Bool.EqualTo(filterBoolKey, filterBoolValue),
								PropertyValidation.Default.Int.EqualTo(filterIntKey, filterIntValue)
							)
					},
					{
						"1 Should be false",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireNone(
								PropertyValidation.Default.Bool.EqualTo(filterBoolKey, filterBoolValue),
								PropertyValidation.Default.Int.EqualTo(filterIntKey, filterIntValue)
							)
					},
					{
						"2 Should be true",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAny(
								PropertyValidation.Default.Bool.EqualTo(filterBoolKey, filterBoolValue),
								PropertyValidation.Default.Int.EqualTo(filterIntKey, filterIntValue)
							)
					},
					{
						"3 Should be true",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAny(
							)
					},
					{
						"4 Should be false",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.Bool.EqualTo(filterBoolKey, !filterBoolValue),
								PropertyValidation.Default.Int.EqualTo(filterIntKey, filterIntValue + 10)
							)
					},
					{
						"5 Should be true",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireNone(
								PropertyValidation.Default.Bool.EqualTo(filterBoolKey, !filterBoolValue),
								PropertyValidation.Default.Int.EqualTo(filterIntKey, filterIntValue + 10)
							)
					},
					{
						"6 Should be false",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAny(
								PropertyValidation.Default.Int.EqualTo(filterIntKey, filterIntValue + 10)
							)
					},
					{
						"7 Should be false",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireNone(
								PropertyValidation.Default.Bool.EqualTo(filterBoolKey, !filterBoolValue),
								PropertyValidation.Default.Int.EqualTo(filterIntKey, filterIntValue + 10)
							)
							.RequireAny(
								PropertyValidation.Default.Int.EqualTo(filterIntKey, filterIntValue + 10)
							)
					},
					{
						"8 Should be true",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireNone(
								PropertyValidation.Default.Bool.EqualTo(filterBoolKey, !filterBoolValue),
								PropertyValidation.Default.Int.EqualTo(filterIntKey, filterIntValue + 10)
							)
							.RequireAny(
								PropertyValidation.Default.Int.EqualTo(filterIntKey, filterIntValue)
							)
					},
					{
						"9 Should be true",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireNone(
								PropertyValidation.Default.Bool.EqualTo(filterBoolKey, !filterBoolValue),
								PropertyValidation.Default.Int.EqualTo(filterIntKey, filterIntValue + 10)
							)
					},
				};

				var filterRes = "filter results:";

				foreach (var kv in filters)
				{
					filterRes += $"\n{kv.Key} : {kv.Value.Validate(item0)}";
				}

				Debug.Log(filterRes);


				// var itemStack0 = itemStore.Create(item0, 20);

				// var clamped = itemInventory0.Modify(
				// 	(item0, 20).WrapInArray(),
				// 	out var addClamp,
				// 	out _
				// );
				//
				// Debug.Log(itemInventory0);
			}

			if (GUILayout.Button("Test Satchel 0c"))
			{
				var itemStore = new ItemStore();
				itemStore.Initialize();

				itemStore.Updated += updateEvent =>
				{
					var res = updateEvent.ToString(ItemStore.Event.Formats.IncludeProperties);
					res += "\n-------- All Items --------\n";
					res += itemStore.ToString(true, true);

					Debug.Log(res);
				};

				var itemInventory0 = new ItemInventory().Initialize(itemStore);
				var itemInventory1 = new ItemInventory().Initialize(itemStore);

				itemInventory0.Updated += itemInventoryEvent =>
				{
					var res = $"Inventory0 Update: {itemInventoryEvent.ToString(ItemInventory.Event.Formats.IncludeStacks)}\n{itemInventory0}";
					Debug.Log(res);
				};

				itemInventory1.Updated += itemInventoryEvent =>
				{
					var res = $"Inventory1 Update: {itemInventoryEvent.ToString(ItemInventory.Event.Formats.IncludeStacks)}\n{itemInventory1}";
					Debug.Log(res);
				};
				
				var filterBoolValue = true;
				var filterBoolKey = "some_bool_key0";
				
				var filterIntValue = 10;
				var filterIntKey = "some_int_key0";
				
				var filterFloatValue = 10f;
				var filterFloatKey = "some_float_key0";
				
				var filterStringValue = "ro";
				var filterStringKey = "some_string_key0";
				
				var item0 = itemStore.New(
					(filterBoolKey, true),
					(filterIntKey, 10),
					(filterFloatKey, 10f),
					(filterStringKey, "rofl")
				);

				var filters = new Dictionary<string, ItemFilter>
				{
					// Bool
					{
						$"{filterBoolKey} EqualTo {filterBoolValue}",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.Bool.EqualTo(filterBoolKey, filterBoolValue)
							)
					},
					// Int
					{
						$"{filterIntKey} EqualTo {filterIntValue}",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.Int.EqualTo(filterIntKey, filterIntValue)
							)
					},
					{
						$"{filterIntKey} LessThan {filterIntValue}",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.Int.LessThan(filterIntKey, filterIntValue)
							)
					},
					{
						$"{filterIntKey} GreaterThan {filterIntValue}",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.Int.GreaterThan(filterIntKey, filterIntValue)
							)
					},
					{
						$"{filterIntKey} LessThanOrEqualTo {filterIntValue}",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.Int.LessThanOrEqualTo(filterIntKey, filterIntValue)
							)
					},
					{
						$"{filterIntKey} GreaterThanOrEqualTo {filterIntValue}",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.Int.GreaterThanOrEqualTo(filterIntKey, filterIntValue)
							)
					},
					// Float
					{
						$"{filterFloatKey} EqualTo {filterFloatValue}",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.Float.EqualTo(filterFloatKey, filterFloatValue)
							)
					},
					{
						$"{filterFloatKey} LessThan {filterFloatValue}",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.Float.LessThan(filterFloatKey, filterFloatValue)
							)
					},
					{
						$"{filterFloatKey} GreaterThan {filterFloatValue}",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.Float.GreaterThan(filterFloatKey, filterFloatValue)
							)
					},
					{
						$"{filterFloatKey} LessThanOrEqualTo {filterFloatValue}",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.Float.LessThanOrEqualTo(filterFloatKey, filterFloatValue)
							)
					},
					{
						$"{filterFloatKey} GreaterThanOrEqualTo {filterFloatValue}",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.Float.GreaterThanOrEqualTo(filterFloatKey, filterFloatValue)
							)
					},
					// String
					{
						$"{filterStringKey} EqualTo {filterStringValue}",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.String.EqualTo(filterStringKey, filterStringValue)
							)
					},
					{
						$"{filterStringKey} Contains {filterStringValue}",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.String.Contains(filterStringKey, filterStringValue)
							)
					},
					{
						$"{filterStringKey} StartsWith {filterStringValue}",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.String.StartsWith(filterStringKey, filterStringValue)
							)
					},
					{
						$"{filterStringKey} EndsWith {filterStringValue}",
						ItemFilterBuilder
							.Begin(itemStore)
							.RequireAll(
								PropertyValidation.Default.String.EndsWith(filterStringKey, filterStringValue)
							)
					},
				};

				var filterRes = "filter results:";

				foreach (var kv in filters)
				{
					filterRes += $"\n{kv.Key} : {kv.Value.Validate(item0)}";
				}

				Debug.Log(filterRes);


				// var itemStack0 = itemStore.Create(item0, 20);

				// var clamped = itemInventory0.Modify(
				// 	(item0, 20).WrapInArray(),
				// 	out var addClamp,
				// 	out _
				// );
				//
				// Debug.Log(itemInventory0);
			}

			if (GUILayout.Button("Test Satchel 0b"))
			{
				var itemStore = new ItemStore();
				itemStore.Initialize();

				Debug.Log(itemStore.Validation.All.Aggregate("Validators", (r, e) => $"{r}\n{e.Key:F}\n\t{e}"));
				
				itemStore.Updated += updateEvent =>
				{
					var res = updateEvent.ToString(ItemStore.Event.Formats.IncludeProperties);
					res += "\n-------- All Items --------\n";
					res += itemStore.ToString(true, true);
					
					Debug.Log(res);
				};
				
				var itemInventory0 = new ItemInventory().Initialize(itemStore);
				var itemInventory1 = new ItemInventory().Initialize(itemStore);

				itemInventory0.Updated += itemInventoryEvent =>
				{
					var res = $"Inventory0 Update: {itemInventoryEvent.ToString(ItemInventory.Event.Formats.IncludeStacks)}\n{itemInventory0}";
					Debug.Log(res);
				};
				
				itemInventory1.Updated += itemInventoryEvent =>
				{
					var res = $"Inventory1 Update: {itemInventoryEvent.ToString(ItemInventory.Event.Formats.IncludeStacks)}\n{itemInventory1}";
					Debug.Log(res);
				};
				
				var item0 = itemStore.New(
					( "some_int_key0", 10)
				);

				var itemStack0 = itemStore.NewStack(item0, 20);

				itemInventory0.Modify(
					(item0, 10).WrapInArray(),
					out var addClamp,
					out _
				);
				
				Debug.Log(addClamp.Aggregate("addClamp", (r, c) => $"{r}\n\t{c.Item.ToString(c.Count)}"));

				// Debug.Log("-----------");
				// Debug.Log("0" + itemInventory0);
				// Debug.Log("1" + itemInventory1);
				// Debug.Log("-----------");
				
				Debug.Log(itemInventory0.Serialize(formatting: Formatting.Indented) + "\n"+itemInventory1.Serialize(formatting: Formatting.Indented));
				
				var hasOverflow = itemInventory0.TransferTo(
					itemInventory1,
					itemStack0.WrapInArray(),
					out var clamped
				);
				
				Debug.Log(clamped.Aggregate("clamped", (r, c) => $"{r}\n\t{c.Item.ToString(c.Count)}"));
				
				Debug.Log(itemInventory0.Serialize(formatting: Formatting.Indented) + "\n"+itemInventory1.Serialize(formatting: Formatting.Indented));
			}
			
			if (GUILayout.Button("Test Satchel 0a"))
			{
				var itemStore = new ItemStore();
				itemStore.Initialize();
				
				itemStore.Updated += updateEvent =>
				{
					var res = updateEvent.ToString(ItemStore.Event.Formats.IncludeProperties);
					res += "\n-------- All Items --------\n";
					res += itemStore.ToString(true, true);
					
					Debug.Log(res);
				};
				
				var itemInventory0 = new ItemInventory().Initialize(itemStore);
				
				var item0 = itemStore.New(
					( "some_int_key0", 10)
				);

				var stack = itemStore.NewStack(item0, 10);

				var item1 = itemStore.New(item0);
				var item2 = itemStore.New(item0, ("some_int_key0", 70));
				var item3 = itemStore.New(item0, ("some_int_key1", 420));
				
				Debug.Log(itemStore.CanStack(item0, item1));
			}
			
			if (GUILayout.Button("Test Satchel 0"))
			{
				var itemStore = new ItemStore();
				itemStore.Initialize();
				
				itemStore.Updated += updateEvent =>
				{
					var res = updateEvent.ToString(ItemStore.Event.Formats.IncludeProperties);
					res += "\n-------- All Items --------\n";
					res += itemStore.ToString(true, true);
					
					Debug.Log(res);
				};
				
				var item0 = itemStore.New(
					( "some_int_key0", 69)
				);

				var stack = itemStore.NewStack(item0, 10);

				var item1 = itemStore.New(item0);
				var item2 = itemStore.New(item0, ("some_int_key0", 70));
				var item3 = itemStore.New(item0, ("some_int_key1", 420));
				
				Debug.Log(itemStore.CanStack(item0, item1));
			}
			
			if (GUILayout.Button("Test Satchel 1"))
			{
				var itemStore = new ItemStore();
				itemStore.Initialize();
				
				itemStore.Updated += updateEvent =>
				{
					var res = updateEvent.ToString(ItemStore.Event.Formats.IncludeProperties);
					res += "\n-------- All Items --------\n";
					res += itemStore.ToString(true, true);
					
					Debug.Log(res);
				};
				
				var item0 = itemStore.New(
					( "some_int_key0", 69)
				);

				var stack = itemStore.NewStack(item0, 10);
				
				itemStore.Destroy(stack);
			}
			
			if (GUILayout.Button("Test Satchel 2"))
			{
				var itemStore = new ItemStore();
				itemStore.Initialize();
				
				itemStore.Updated += updateEvent =>
				{
					var res = updateEvent.ToString(ItemStore.Event.Formats.IncludeProperties);
					res += "\n-------- All Items --------\n";
					res += itemStore.ToString(true, true);
					
					Debug.Log(res);
				};
				
				var item0 = itemStore.New(
					( "some_int_key0", 69)
				);
				
				var item1 = itemStore.New(
					( "some_int_key0", 69)
				);
				
				var item2 = itemStore.New(
					( "some_int_key0", 69),
					( "some_int_key1", 420)
				);

				Debug.Log($"Can stack {item0.Id} and {item1.Id} : {itemStore.CanStack(item0, item1)}");
				Debug.Log($"Can stack {item0.Id} and {item2.Id} : {itemStore.CanStack(item0, item2)}");

				item0.Set("some_int_key1", 420);
				
				Debug.Log($"Can stack {item0.Id} and {item2.Id} : {itemStore.CanStack(item0, item2)}");

				item0.Set(Constants.InstanceCount, 10);
				
				Debug.Log($"Can stack {item0.Id} and {item2.Id} : {itemStore.CanStack(item0, item2)}");
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