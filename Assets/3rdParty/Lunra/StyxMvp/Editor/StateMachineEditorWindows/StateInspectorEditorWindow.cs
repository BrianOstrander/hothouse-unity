using UnityEditor;
using UnityEngine;

using Lunra.Core;
using Lunra.Editor.Core;
using Lunra.StyxMvp.Services;

namespace Lunra.StyxMvp
{
	public class StateInspectorEditorWindow : StateMachineEditorWindow
	{
		EditorPrefsFloat entryScroll;

		[MenuItem("Window/Styx/State Inspector")]
		static void Initialize() { OnInitialize<StateInspectorEditorWindow>("State Inspector"); }

		public StateInspectorEditorWindow() : base("Lunra.Styx.StateInspector.")
		{
			entryScroll = new EditorPrefsFloat(KeyPrefix + "EntryScroll");

			AppInstantiated += OnAppInstantiated;
			Gui += OnStateGui;
		}

		#region Events
		void OnAppInstantiated()
		{
			App.Heartbeat.Update += OnHeartbeatUpdate;
		}

		void OnHeartbeatUpdate()
		{
			if (StateMachine.CaptureTraceData.Value) Repaint();
		}

		void OnStateGui()
		{
			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();
				StateMachine.LogStateChanges.Value = EditorGUILayoutExtensions.ToggleButtonCompact(
					"Logging States",
					StateMachine.LogStateChanges.Value,
					options: GUILayout.MinWidth(128f)
				);
				StateMachine.CaptureTraceData.Value = EditorGUILayoutExtensions.ToggleButtonCompact(
					"Capturing Traces",
					StateMachine.CaptureTraceData.Value,
					options: GUILayout.MinWidth(128f)
				);
			}
			EditorGUILayout.EndHorizontal();
			
			var isActive = Application.isPlaying && App.HasInstance && App.S != null;

			if (!isActive)
			{
				EditorGUILayout.HelpBox("Only available during playmode.", MessageType.Info);
				return;
			}

			if (StateMachine.CaptureTraceData.Value) EditorGUILayout.SelectableLabel("Currently " + App.S.CurrentState + "." + App.S.CurrentEvent, EditorStyles.boldLabel);
			else GUILayout.Label("Enable \"Capturing Traces\" to inspect StateMachine stack.");

			if (!StateMachine.CaptureTraceData.Value) return;

			var entries = App.S.GetEntries();

			if (entries.Length == 0)
			{
				EditorGUILayout.HelpBox("No entries to display.", MessageType.Info);
				return;
			}

			entryScroll.VerticalScroll = EditorGUILayout.BeginScrollView(entryScroll.VerticalScroll);
			{
				foreach (var entry in entries)
				{
					EditorGUILayout.BeginVertical(EditorStyles.helpBox);
					{
						var isUnrecognizedState = false;

						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayoutExtensions.RichTextLabel(entry.Trace.GetReadableName());
							
							// entry.Trace.GetReadableCallback()
							
							var stateColor = Color.black;

							switch (entry.EntryState)
							{
								case StateMachine.EntryStates.Queued: stateColor = Color.gray.NewB(0.2f); break;
								case StateMachine.EntryStates.Waiting: stateColor = Color.gray.NewB(0.5f); break; // Waiting for state
								case StateMachine.EntryStates.Calling: stateColor = Color.white; break;
								case StateMachine.EntryStates.Blocking: stateColor = Color.red; break;
								case StateMachine.EntryStates.Blocked: stateColor = Color.red.NewV( 0.7f); break;
								default:
									isUnrecognizedState = true;
									break;
							}

							GUIExtensions.PushColor(stateColor);
							{
								GUILayout.Label(entry.EntryState.ToString(), EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
							}
							GUIExtensions.PopColor();
						}
						EditorGUILayout.EndHorizontal();
						
						GUIExtensions.PushEnabled(entry.Trace.IsValid);
						{
							var buttonLabel = entry.Trace.IsValid ? AssetDatabaseExtensions.GetRelativeAssetPath(entry.Trace.InitializerFilePath) + " : " + entry.Trace.InitializerFileLine : "No file to open";
							if (GUILayout.Button(buttonLabel, EditorStyles.linkLabel))
							{
								AssetDatabase.OpenAsset(
									AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabaseExtensions.GetRelativeAssetPath(entry.Trace.InitializerFilePath)),
									entry.Trace.InitializerFileLine
								);
							}
						}
						GUIExtensions.PopEnabled();

						if (!string.IsNullOrEmpty(entry.SynchronizedId)) GUILayout.Label("Synchronized Id: " + entry.SynchronizedId);

						if (isUnrecognizedState) EditorGUILayout.HelpBox("Unrecognized EntryState: " + entry.EntryState, MessageType.Error);
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			EditorGUILayout.EndScrollView();
		}
		#endregion
	}
}