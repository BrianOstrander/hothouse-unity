using System;
using System.Collections.Generic;

using UnityEngine;
using Object = UnityEngine.Object;

using LunraGames.SubLight.Models;

namespace LunraGames.SubLight
{
	public class App
	{
		static App instance;

		public static bool HasInstance { get { return instance != null; } }

		/// <summary>
		/// Called when the App instance is done being constructed.
		/// </summary>
		/// <remarks>
		/// Idealy nothing should use this except editor time scripts.
		/// </remarks>
		public static Action<App> Instantiated = ActionExtensions.GetEmpty<App>();

		Main main;
		public static Main Main { get { return instance.main; } }

		CallbackService callbackService;
		public static CallbackService Callbacks { get { return instance.callbackService; } }

		Heartbeat heartbeat;
		public static Heartbeat Heartbeat { get { return instance.heartbeat; } }

		IModelMediator modelMediator;
		public static IModelMediator M { get { return instance.modelMediator; } }

		PresenterMediator presenterMediator;
		public static PresenterMediator P { get { return instance.presenterMediator; } }

		ViewMediator viewMediator;
		public static ViewMediator V { get { return instance.viewMediator; } }

		AudioService audioService;
		public static AudioService Audio { get { return instance.audioService; } }

		StateMachine stateMachine;
		public static StateMachine SM { get { return instance.stateMachine; } }

		SceneService scenes;
		public static SceneService Scenes { get { return instance.scenes; } }

		List<GameObject> defaultViews;

		// TODO: Should this be here?
		Transform canvasRoot;
		public static Transform CanvasRoot { get { return instance.canvasRoot; } }
		Transform gameCanvasRoot;
		public static Transform GameCanvasRoot { get { return instance.gameCanvasRoot; } }
		Transform overlayCanvasRoot;
		public static Transform OverlayCanvasRoot { get { return instance.overlayCanvasRoot; } }

		PreferencesModel preferences;
		/// <summary>
		/// Gets the current preferences.
		/// </summary>
		/// <remarks>
		/// Feel free to hook onto the Changed listeners for this model, they're
		/// preserved when saving. Don't provide this model to any services
		/// before initialization though, since it will be replaced.
		/// </remarks>
		/// <value>The preferences.</value>
		public static PreferencesModel Preferences { get { return instance.preferences; } }

		static Func<PreferencesModel> CurrentPreferences { get { return () => Preferences; } }

		public App(
			Main main, 
			List<GameObject> defaultViews, 
			GameObject audioRoot, 
			Transform canvasRoot,
			Transform gameCanvasRoot,
			Transform overlayCanvasRoot,
			AudioConfiguration audioConfiguration
		)
		{
			instance = this;
			this.main = main;
			this.defaultViews = defaultViews;
			this.canvasRoot = canvasRoot;
			this.gameCanvasRoot = gameCanvasRoot;
			this.overlayCanvasRoot = overlayCanvasRoot;
			callbackService = new CallbackService();
			heartbeat = new Heartbeat();
			presenterMediator = new PresenterMediator(Heartbeat);
			viewMediator = new ViewMediator(Heartbeat, Callbacks);
			stateMachine = new StateMachine(
				Heartbeat,
				new InitializeState(),
				new TransitionState()
			);
			
			if (Application.isEditor)
			{
#if UNITY_EDITOR
				modelMediator = new DesktopModelMediator();
#endif
			}
			else if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.WindowsPlayer)
			{
				modelMediator = new DesktopModelMediator();
			}
			else
			{
				throw new Exception("Unknown platform");
			}

			scenes = new SceneService(Callbacks);
			
			audioService = new AudioService(
				audioRoot, 
				audioConfiguration, 
				Heartbeat
			);

			Instantiated(this);
		}

		public static void Restart(string message)
		{
			Debug.LogError("NO RESTART LOGIC DEFINED - TRIGGERED BY:\n" + message);
		}

		#region Global setters
		/// <summary>
		/// Used to set the initial preferences. Should only be set once from
		/// the initialize state.
		/// </summary>
		/// <param name="preferences">Preferences.</param>
		public static void SetPreferences(PreferencesModel preferences) { instance.preferences = preferences; }
		#endregion

		#region MonoBehaviour events

		public void Awake()
		{
			var payload = new InitializePayload();
			payload.DefaultViews = defaultViews;
			stateMachine.RequestState(payload);
		}

		public void Start()
		{

		}

		public void Update(float delta)
		{
			heartbeat.Update(delta);
		}

		public void LateUpdate(float delta)
		{
			heartbeat.LateUpdate(delta);
		}

		public void FixedUpdate() { }

		public void OnApplicationPause(bool paused) { }

		public void OnApplicationQuit() { }

		#endregion
	}
}
