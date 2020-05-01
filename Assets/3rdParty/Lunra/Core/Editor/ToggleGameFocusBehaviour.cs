using UnityEditor;

namespace Lunra.Editor.Core
{
	[InitializeOnLoad]
	public static class ToggleGameFocusBehaviour
	{
		static EditorPrefsBool isFocusSceneEnabled = new EditorPrefsBool("Lunra.Core.ToggleGameFocusBehaviour.IsFocusSceneEnabled");
		static int? focusSceneDelay;
		
		static ToggleGameFocusBehaviour()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			EditorApplication.pauseStateChanged += OnPauseStateChanged;
			EditorApplication.update += OnUpdate;
		}

		static void OnPlayModeStateChanged(PlayModeStateChange playModeState)
		{
			switch (playModeState)
			{
				case PlayModeStateChange.EnteredPlayMode:
					focusSceneDelay = 2;
					break;
			}
		}

		static void OnPauseStateChanged(PauseState pauseState)
		{
			switch (pauseState)
			{
				case PauseState.Unpaused:
					focusSceneDelay = 45;
					break;
			}
		}

		static void OnUpdate()
		{
			if (!EditorApplication.isPlayingOrWillChangePlaymode) return;
			if (!focusSceneDelay.HasValue) return;

			focusSceneDelay--;

			if (0 < focusSceneDelay) return;
			
			focusSceneDelay = null;
			EditorWindow.FocusWindowIfItsOpen<SceneView>();
		}

		[MenuItem("Edit/Focus On Play/Game")]
		static void FocusGame() => isFocusSceneEnabled.Value = false;
		
		[MenuItem("Edit/Focus On Play/Game", true)]
		static bool IsFocusGameEnabled() => isFocusSceneEnabled.Value;

		[MenuItem("Edit/Focus On Play/Scene")]
		static void FocusScene() => isFocusSceneEnabled.Value = true;

		[MenuItem("Edit/Focus On Play/Scene", true)]
		static bool IsFocusSceneEnabled() => !isFocusSceneEnabled.Value;
	}
}