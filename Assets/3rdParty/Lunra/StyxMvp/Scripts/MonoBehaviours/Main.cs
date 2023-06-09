using System;
using Lunra.StyxMvp.Models;
using UnityEngine;
using AudioConfiguration = Lunra.StyxMvp.Services.AudioConfiguration;
using Lunra.StyxMvp.Services;

namespace Lunra.StyxMvp 
{
	/// <summary>
	/// Main kickstarts the App class and provides MonoBehaviour functionality without the annoying life cycle
	/// constraints of an actual MonoBehaviour.
	/// </summary>
	/// <remarks>
	/// This class should never be called directly, it simply gets the App singleton going, and passes any unity 
	/// specific events back to it.
	/// </remarks>
	public abstract class Main : MonoBehaviour
	{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] AudioConfiguration audioConfiguration;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null

		App app;
		
		void Awake()
		{
			app = new App(
				this,
				audioConfiguration,
				InstantiateStates,
				InstantiateModelMediator,
				OnStartupDone
			);
			DontDestroyOnLoad(gameObject);
			app.Awake();
		}

		void Start() => app.Start();

		void Update() => app.Update();

		void LateUpdate() => app.LateUpdate(); 

		void FixedUpdate() => app.FixedUpdate();

		void OnApplicationPause(bool paused) => app.OnApplicationPause(paused); 

		void OnApplicationQuit() => app.OnApplicationQuit();

		void OnDrawGizmos() => app?.OnDrawGizmos();

		protected virtual IModelMediator InstantiateModelMediator() => new DesktopModelMediator();

		protected abstract IState[] InstantiateStates();
		
		protected abstract void OnStartupDone();
	}
}
