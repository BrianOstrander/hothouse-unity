using System;

using UnityEditor;
using UnityEngine;

namespace LunraGames.SubLight
{
	public abstract class StateMachineEditorWindow : EditorWindow
	{
		protected readonly string KeyPrefix;

		protected Action Enable = ActionExtensions.Empty;
		protected Action Disable = ActionExtensions.Empty;
		protected Action Gui = ActionExtensions.Empty;
		protected Action EditorUpdate = ActionExtensions.Empty;

		protected Action<PlayModeStateChange> PlayModeState = ActionExtensions.GetEmpty<PlayModeStateChange>();
		protected Action AppInstantiated = ActionExtensions.Empty;
		protected Action AppDestroyed = ActionExtensions.Empty;
		protected Action<StateChange> StateChange = ActionExtensions.GetEmpty<StateChange>();

		protected static void OnInitialize<T>(string windowName) where T : StateMachineEditorWindow
		{
			GetWindow(typeof(T), false, windowName).Show();
		}

		public StateMachineEditorWindow(string keyPrefix)
		{
			KeyPrefix = keyPrefix;
		}

		#region Events
		void OnAppInstantiated(App app)
		{
			App.Callbacks.StateChange += StateChange;
			AppInstantiated();
		}

		void OnEnable()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode)
			{
				App.Instantiated += OnAppInstantiated;
			}

			EditorApplication.playModeStateChanged += OnPlayModeState;

			Enable();
		}

		void OnDisable()
		{
			Disable();
		}

		void OnGUI()
		{
			Gui();
		}

		void Update()
		{
			EditorUpdate();
		}

		void OnPlayModeState(PlayModeStateChange state)
		{
			switch (state)
			{
				case PlayModeStateChange.ExitingPlayMode:
					App.Instantiated -= OnAppInstantiated;
					AppDestroyed();
					break;
			}

			PlayModeState(state);
		}
		#endregion
	}
}