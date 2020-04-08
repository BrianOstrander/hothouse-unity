using UnityEngine;

namespace LunraGames.SubLight 
{
	/// <summary>
	/// Main kickstarts the App class and provides MonoBehaviour functionality without the annoying life cycle
	/// constraints of an actual MonoBehaviour.
	/// </summary>
	/// <remarks>
	/// This class should never be called directly, it simply gets the App singleton going, and passes any unity 
	/// specific events back to it.
	/// </remarks>
	public class Main : MonoBehaviour 
	{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField]
		GameObject audioRoot;
		[SerializeField]
		Transform canvasRoot;
		[SerializeField]
		Transform gameCanvasRoot;
		[SerializeField]
		Transform overlayCanvasRoot;
		[SerializeField]
		DefaultViews defaultViews;
		[SerializeField]
		AudioConfiguration audioConfiguration;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null

		App app;
		
		void Awake() 
		{
			app = new App(
				this, 
				defaultViews.Prefabs,
				audioRoot, 
				canvasRoot,
				gameCanvasRoot,
				overlayCanvasRoot,
				audioConfiguration
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
	}
}
