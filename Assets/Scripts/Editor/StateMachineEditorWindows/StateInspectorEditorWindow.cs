using System;

using UnityEditor;
using UnityEngine;

using LunraGamesEditor;

namespace LunraGames.SubLight
{
	public class StateInspectorEditorWindow : StateMachineEditorWindow
	{
		EditorPrefsFloat entryScroll;
		EditorPrefsBool isInspecting;

		[MenuItem("Window/SubLight/State Inspector")]
		static void Initialize() { OnInitialize<StateInspectorEditorWindow>("State Inspector"); }

		public StateInspectorEditorWindow() : base("LG_SL_StateInspector_")
		{
			entryScroll = new EditorPrefsFloat(KeyPrefix + "EntryScroll");
			isInspecting = new EditorPrefsBool(KeyPrefix + "IsInspecting");

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
			if (isInspecting) Repaint();
		}

		void OnStateGui()
		{
			var isActive = Application.isPlaying && App.HasInstance && App.SM != null;

			if (!isActive)
			{
				EditorGUILayout.HelpBox("Only available during playmode.", MessageType.Info);
				EditorGUILayout.BeginHorizontal();
				{
					GUILayout.FlexibleSpace();
					isInspecting.Value = EditorGUILayout.Toggle("Is Inspecting", isInspecting.Value, GUILayout.ExpandWidth(false));
				}
				EditorGUILayout.EndHorizontal();
				return;
			}

			EditorGUILayout.BeginHorizontal();
			{
				if (isInspecting) EditorGUILayout.SelectableLabel("Currently " + App.SM.CurrentState + "." + App.SM.CurrentEvent, EditorStyles.boldLabel);
				else GUILayout.Label("Enable inspection to view StateMachine updates.");

				isInspecting.Value = EditorGUILayout.Toggle("Is Inspecting", isInspecting.Value, GUILayout.ExpandWidth(false));
			}
			EditorGUILayout.EndHorizontal();

			if (!isInspecting.Value) return;

			var entries = App.SM.GetEntries();

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
							EditorGUILayout.SelectableLabel(entry.Description);
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

							EditorGUILayoutExtensions.PushColor(stateColor);
							{
								GUILayout.Label(entry.EntryState.ToString(), EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
							}
							EditorGUILayoutExtensions.PopColor();
						}
						EditorGUILayout.EndHorizontal();

						GUILayout.Label("Syncronized Id: " + (entry.SynchronizedId ?? "< null >"));

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