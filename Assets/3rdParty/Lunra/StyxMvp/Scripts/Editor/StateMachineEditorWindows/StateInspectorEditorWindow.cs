using Lunra.Core;
using UnityEditor;
using UnityEngine;

using Lunra.Editor.Core;

namespace Lunra.StyxMvp
{
	public class StateInspectorEditorWindow : StateMachineEditorWindow
	{
		EditorPrefsFloat entryScroll;

		[MenuItem("Window/SubLight/State Inspector")]
		static void Initialize() { OnInitialize<StateInspectorEditorWindow>("State Inspector"); }

		public StateInspectorEditorWindow() : base("LG_SL_StateInspector_")
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

		void OnHeartbeatUpdate(float delta)
		{
			if (StateMachine.CapturingTraceData.Value) Repaint();
		}

		void OnStateGui()
		{
			var isActive = Application.isPlaying && App.HasInstance && App.S != null;

			if (!isActive)
			{
				EditorGUILayout.HelpBox("Only available during playmode.", MessageType.Info);
				EditorGUILayout.BeginHorizontal();
				{
					GUILayout.FlexibleSpace();
					StateMachine.CapturingTraceData.Value = EditorGUILayout.Toggle(
						"Is Inspecting",
						StateMachine.CapturingTraceData.Value,
						GUILayout.ExpandWidth(false)
					);
				}
				EditorGUILayout.EndHorizontal();
				return;
			}

			EditorGUILayout.BeginHorizontal();
			{
				if (StateMachine.CapturingTraceData.Value) EditorGUILayout.SelectableLabel("Currently " + App.S.CurrentState + "." + App.S.CurrentEvent, EditorStyles.boldLabel);
				else GUILayout.Label("Enable inspection to view StateMachine updates.");

				StateMachine.CapturingTraceData.Value = EditorGUILayout.Toggle("Is Inspecting", StateMachine.CapturingTraceData.Value, GUILayout.ExpandWidth(false));
			}
			EditorGUILayout.EndHorizontal();

			if (!StateMachine.CapturingTraceData.Value) return;

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
							GUILayout.Label(entry.Trace.ToString());
							
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
							var buttonLabel = entry.Trace.IsValid ? AssetDatabaseExtensions.GetRelativeAssetPath(entry.Trace.FilePath) + " : " + entry.Trace.FileLine : "No file to open";
							if (GUILayout.Button(buttonLabel, EditorStyles.linkLabel))
							{
								AssetDatabase.OpenAsset(
									AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabaseExtensions.GetRelativeAssetPath(entry.Trace.FilePath)),
									entry.Trace.FileLine
								);
							}
						}
						GUIExtensions.PopEnabled();

						GUILayout.Label("Synchronized Id: " + (entry.SynchronizedId ?? "< null >"));

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