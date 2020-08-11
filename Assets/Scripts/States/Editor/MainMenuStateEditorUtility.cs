using Lunra.Hothouse.Models;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Services.Editor
{
	[InitializeOnLoad]
	public static class MainMenuStateEditorUtility
	{
		static MainMenuState cachedState;
		
		public static bool GetMainMenuState(out MainMenuState state)
		{
			state = null;
			if (!Application.isPlaying || !App.HasInstance || App.S == null) return false;
			if (!App.S.Is(typeof(MainMenuState), StateMachine.Events.Begin, StateMachine.Events.Idle)) return false;

			state = cachedState ?? (cachedState = App.S.CurrentHandler as MainMenuState);
			return true;
		}

		public static bool GetMainMenu(out MainMenuModel model, out MainMenuState state)
		{
			if (GetMainMenuState(out state))
			{
				model = state.Payload.MainMenu;
				return true;
			}

			model = null;
			return false;
		}
		
		static MainMenuStateEditorUtility()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		static void OnPlayModeStateChanged(PlayModeStateChange playModeState)
		{
			switch (playModeState)
			{
				case PlayModeStateChange.ExitingEditMode:
				case PlayModeStateChange.ExitingPlayMode:
					cachedState = null;
					break;
			}
		}
	}
}