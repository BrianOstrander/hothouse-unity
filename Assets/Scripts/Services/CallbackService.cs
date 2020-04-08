using System;

using UnityEngine.SceneManagement;

namespace LunraGames.SubLight
{
	public class CallbackService
	{
		#region Events
		/// <summary>
		/// When the escape key is released.
		/// </summary>
		public Action Escape = ActionExtensions.Empty;
		/// <summary>
		/// A scene loaded.
		/// </summary>
		public Action<Scene, LoadSceneMode> SceneLoad = ActionExtensions.GetEmpty<Scene, LoadSceneMode>();
		/// <summary>
		/// A scene unloaded.
		/// </summary>
		public Action<Scene> SceneUnload = ActionExtensions.GetEmpty<Scene>();
		/// <summary>
		/// A scene set active.
		/// </summary>
		public Action<Scene, Scene> SceneSetActive = ActionExtensions.GetEmpty<Scene, Scene>();
		/// <summary>
		/// The state changed.
		/// </summary>
		public Action<StateChange> StateChange = ActionExtensions.GetEmpty<StateChange>();
		/// <summary>
		/// Called when any interactable elemnts are added to the world, and 
		/// false when all are removed.
		/// </summary>
		public Action<bool> Interaction = ActionExtensions.GetEmpty<bool>();
		#endregion

		// TODO: Think about moving these to state or GameModel...

		
		public CallbackService()
		{
			SceneManager.sceneLoaded += (scene, loadMode) => SceneLoad(scene, loadMode);
			SceneManager.sceneUnloaded += scene => SceneUnload(scene);
			SceneManager.activeSceneChanged += (currentScene, nextScene) => SceneSetActive(currentScene, nextScene);
		}
	}
}