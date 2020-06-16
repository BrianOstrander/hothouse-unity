using Lunra.Hothouse.Models;
using Lunra.Hothouse.Services;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Services;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Editor
{
	[InitializeOnLoad]
	public static class SettingsProviderCache
	{
		static GameState gameState;
		
		public static bool GetGameState(out GameState gameState)
		{
			gameState = null;
			if (!Application.isPlaying || !App.HasInstance || App.S == null) return false;
			if (!App.S.Is(typeof(GameState), StateMachine.Events.Begin, StateMachine.Events.Idle)) return false;

			gameState = SettingsProviderCache.gameState ?? (SettingsProviderCache.gameState = App.S.CurrentHandler as GameState);
			return true;
		}

		public static bool GetGame(out GameModel game)
		{
			if (GetGameState(out var gameState))
			{
				game = gameState.Payload.Game;
				return true;
			}

			game = null;
			return false;
		}
		
		static SettingsProviderCache()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		static void OnPlayModeStateChanged(PlayModeStateChange playModeState)
		{
			switch (playModeState)
			{
				case PlayModeStateChange.ExitingEditMode:
				case PlayModeStateChange.ExitingPlayMode:
					gameState = null;
					break;
			}
		}
	}
}