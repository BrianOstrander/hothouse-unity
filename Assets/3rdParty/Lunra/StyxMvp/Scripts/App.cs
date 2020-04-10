using System;

using UnityEngine;

using Lunra.Core;
using Lunra.StyxMvp.Models;
using Lunra.StyxMvp.Presenters;
using Lunra.StyxMvp.Services;
using AudioConfiguration = Lunra.StyxMvp.Services.AudioConfiguration;

namespace Lunra.StyxMvp
{
	public class App
	{
		static App instance;

		public static bool HasInstance => instance != null;

		/// <summary>
		/// Called when the App instance is done being constructed.
		/// </summary>
		/// <remarks>
		/// Ideally nothing should use this except editor time scripts.
		/// </remarks>
		public static Action<App> Instantiated = ActionExtensions.GetEmpty<App>();

		Main main;
		public static Main Main => instance.main;

		Heartbeat heartbeat;
		public static Heartbeat Heartbeat => instance.heartbeat;

		IModelMediator modelMediator;
		public static IModelMediator M => instance.modelMediator;

		PresenterMediator presenterMediator;
		public static PresenterMediator P => instance.presenterMediator;

		ViewMediator viewMediator;
		public static ViewMediator V => instance.viewMediator;

		Audio audio;
		public static Audio Audio => instance.audio;

		StateMachine stateMachine;
		public static StateMachine S => instance.stateMachine;

		Scenes scenes;
		public static Scenes Scenes => instance.scenes;

		public App(
			Main main,
			AudioConfiguration audioConfiguration
		)
		{
			instance = this;
			this.main = main;
			heartbeat = new Heartbeat();
			presenterMediator = new PresenterMediator(Heartbeat);
			viewMediator = new ViewMediator(
				Main.transform,
				Heartbeat
			);
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

			scenes = new Scenes();
			
			audio = new Audio(
				Main.transform,
				audioConfiguration,
				Heartbeat
			);

			Instantiated(this);
		}

		public static void Restart(string message)
		{
			Debug.LogError("NO RESTART LOGIC DEFINED - TRIGGERED BY:\n" + message);
		}

		#region MonoBehaviour events

		public void Awake()
		{
			var payload = new InitializePayload();
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
