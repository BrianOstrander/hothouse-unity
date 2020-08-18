using Lunra.Hothouse.Models;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Services.Editor
{
	[InitializeOnLoad]
	public static class GameStateEditorUtility
	{
		static GameState cachedState;
		
		public static bool GetGameState(out GameState state)
		{
			state = null;
			if (!Application.isPlaying || !App.HasInstance || App.S == null) return false;
			if (!App.S.Is(typeof(GameState), StateMachine.Events.Begin, StateMachine.Events.Idle)) return false;

			state = cachedState ?? (cachedState = App.S.CurrentHandler as GameState);
			return true;
		}

		public static bool GetGame(out GameModel model, out GameState state)
		{
			if (GetGameState(out state))
			{
				model = state.Payload.Game;
				return true;
			}

			model = null;
			return false;
		}
		
		static GameStateEditorUtility()
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