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
				States,
				OnStartupDone
			);
			DontDestroyOnLoad(gameObject);
			app.Awake();
		}

		void Start() 
		{
			app.Start ();
		}

		void Update() 
		{
			app.Update(Time.deltaTime);
		}

		void LateUpdate()
		{
			app.LateUpdate(Time.deltaTime);
		}

		void FixedUpdate() 
		{
			app.FixedUpdate();	
		}

		void OnApplicationPause(bool paused) 
		{
			app.OnApplicationPause(paused);
		}

		void OnApplicationQuit() 
		{
			app.OnApplicationQuit();
		}

		protected abstract IState[] States { get; }
		protected abstract void OnStartupDone();
	}
}
