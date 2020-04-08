using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using Object = UnityEngine.Object;

using LunraGames.SubLight.Models;

namespace LunraGames.SubLight
{
	public class ViewMediator
	{
		Heartbeat heartbeat;
		CallbackService callbacks;

		int interactionCount;
		public int InteractionCount
		{
			get { return interactionCount; }
			set
			{
				var count = Mathf.Max(0, value);
				var oldCount = interactionCount;
				interactionCount = count;
#if UNITY_EDITOR
				if (UnityEngine.Application.isPlaying)
#endif
				{
					// if last button is disabled or first button is enabled
					if ((count == 0 || oldCount == 0) && count != oldCount)
					{
						App.Callbacks.Interaction(count != 0);
					}
				}
			}
		}
		List<IView> pool = new List<IView>();
		List<GameObject> defaultViews = new List<GameObject>();
		List<IView> views = new List<IView>();
		Transform storage;

		public ViewMediator(Heartbeat heartbeat, CallbackService callbacks)
		{
			if (heartbeat == null) throw new ArgumentNullException("heartbeat");
			if (callbacks == null) throw new ArgumentNullException("callbacks");

			this.heartbeat = heartbeat;
			this.callbacks = callbacks;
		}

		public void Initialize(List<GameObject> defaultViews, Transform viewStorage, Action<RequestStatus> done)
		{
			pool = new List<IView>();
			this.defaultViews = defaultViews;
			storage = viewStorage;
			foreach (var prefab in defaultViews)
			{
				var prefabView = prefab.GetComponent<IView>();
				if (prefabView == null)
				{
					Debug.LogError("View prefab \"" + prefab.name + "\" has no root IView component.");
					continue;
				}
				for (var i = 0; i < Mathf.Max(prefabView.PoolSize, 1); i++)
				{
					Pool(Create(prefab));
				}
			}
			heartbeat.Update += Update;
			heartbeat.LateUpdate += LateUpdate;

			done(RequestStatus.Success);
		}

		/// <summary>
		/// Get a new or pooled view
		/// </summary>
		/// <typeparam name="V">Type of view.</typeparam>
		public V Get<V>(Func<V, bool> predicate = null) where V : class, IView
		{
			Func<IView, bool> defaultPredicate = null;
			if (predicate != null)
			{
				defaultPredicate = v =>
				{
					V typed;
					try { typed = v as V; }
					catch { return false; }
					if (typed == null) return false;
					return predicate(typed);
				};
			}
			return Get(typeof(V), defaultPredicate) as V;
		}

		/// <summary>
		/// Get a new or pooled view
		/// </summary>
		/// <param name="type">Type of view.</param>
		public IView Get(Type type, Func<IView, bool> predicate = null)
		{
			IView existing = null;
			foreach (var view in pool)
			{
				if (!type.IsAssignableFrom(view.GetType())) continue;
				if (predicate != null)
				{
					try
					{
						if (!predicate(view)) continue;
					}
					catch (Exception e)
					{
						Debug.LogException(e);
						continue;
					}
				}
				existing = view;
				break;
			}

			if (existing != null)
			{
				pool.Remove(existing);
				return existing;
			}
			return Create(type, predicate);
		}

		IView Create(Type type, Func<IView, bool> predicate = null)
		{
			GameObject prefab = null;
			foreach (var view in defaultViews)
			{
				var component = view.GetComponent(type);
				if (component == null) continue;
				if (predicate != null)
				{
					try
					{
						if (!predicate(component as IView)) continue;
					}
					catch (Exception e)
					{
						Debug.LogException(e);
						continue;
					}
				}
				prefab = view;
				break;
			}
			if (prefab == null)
			{
				Debug.LogError("No view prefab with a root component implimenting " + type.FullName);
				return null;
			}
			return Create(prefab);
		}

		IView Create(GameObject prefab)
		{
			var spawned = Object.Instantiate(prefab).GetComponent<IView>();
			spawned.gameObject.SetActive(false);
			spawned.transform.SetParent(storage);

			return spawned;
		}

		/// <summary>
		/// Return a view to the pool of views available for assignment to new presenters.
		/// </summary>
		/// <remarks>
		/// This should only be called from Presenter, or if you know what you're doing.
		/// </remarks>
		/// <param name="view">View.</param>
		public void Pool(IView view)
		{
			if (view == null)
			{
				Debug.LogError("Can't pool a null view");
				return;
			}
			if (pool.Contains(view))
			{
				Debug.LogError("Pool already contains the view " + view.gameObject.name);
				return;
			}
			if (view.Visible) Debug.LogError("Pooling a visible view, this shouldn't happen, and may cause unintended side effects");
			pool.Add(view);
		}

		void Closing(IView view)
		{
			// TODO: make this take into account multiple calls per frame, because Time.deltaTime is going to ruin it.
			var progress = Mathf.Min(view.CloseDuration, view.Progress + Time.deltaTime);
			var scalar = progress / view.CloseDuration;

			view.SetProgress(progress, scalar);

			view.Closing(scalar);
			if (Mathf.Approximately(1f, scalar))
			{
				views.Remove(view);

				view.Parent = null;
				DisableAndCacheView(view);
				view.Closed();
			}
		}

		void Showing(IView view)
		{
			// TODO: make this take into account multiple calls per frame, because Time.deltaTime is going to ruin it.
			var progress = Mathf.Min(view.ShowDuration, view.Progress + Time.deltaTime);
			var scalar = progress / view.ShowDuration;

			view.SetProgress(progress, scalar);

			view.Showing(scalar);
			if (Mathf.Approximately(1f, scalar))
			{
				view.Shown();
			}
		}

		void Update(float delta)
		{
			FrameCount++;
			CameraHasMoved = !lastCameraForward.Approximately(CameraForward) || !lastCameraPosition.Approximately(CameraPosition);
			lastCameraForward = CameraForward;
			lastCameraPosition = CameraPosition;

			foreach (var view in views.ToList())
			{
				if (view.TransitionState != TransitionStates.Closed) view.Constant(delta);

				if (view.TransitionState == TransitionStates.Shown)
				{
					view.Idle(delta);
					continue;
				}

				var unmodifiedView = view;
				if (unmodifiedView.TransitionState == TransitionStates.Showing) Showing(unmodifiedView);
				else if (unmodifiedView.TransitionState == TransitionStates.Closing) Closing(unmodifiedView);
				else
				{
					var error = "The view " + (unmodifiedView == null ? "null" : unmodifiedView.gameObject.name) + " with state " + unmodifiedView.TransitionState + " is still on the waldo, this should not be possible";
					Debug.LogError(error);
					views.Remove(unmodifiedView);
				}
			}
		}

		void LateUpdate(float delta)
		{
			foreach (var view in views.ToList())
			{
				if (view.TransitionState != TransitionStates.Closed) view.LateConstant(delta);
				if (view.TransitionState == TransitionStates.Shown) view.LateIdle(delta);
			}
		}

		void DisableAndCacheView(IView view)
		{
#if UNITY_EDITOR
			if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
				return;
#endif
			view.gameObject.SetActive(false);
			view.transform.SetParent(storage);
		}

		public static bool Initialized { get { throw new NotImplementedException(); } }

		public void Show(IView view, bool instant = false, Transform parent = null)
		{
			if (view.Visible)
			{
				return;
			}

			view.Parent = parent;

			if (instant) view.SetProgress(view.ShowDuration, 1f);
			else view.SetProgress(0f, 0f);

			views.Add(view);
			view.Prepare();
			// Call showing here since we want instantanious shows to actually be instantanious.
			Showing(view);
		}

		public void Close(IView view, bool instant = false)
		{
			if (view == null) throw new ArgumentNullException("view");

			switch (view.TransitionState)
			{
				case TransitionStates.Closed:
					return;
				case TransitionStates.Unknown: // This may no longer ever get called.
					Debug.LogWarning("Can't close a view with an unknown state", view.gameObject);
					return;
			}

			if (instant) view.SetProgress(view.CloseDuration, 1f);
			else view.SetProgress(0f, 0f);

			view.PrepareClose();
			Closing(view);
		}

		#region Animation Data
		// Instead of animations getting their data from weird places, they should get everything through here.

		Vector3 lastCameraPosition;
		Vector3 lastCameraForward;

		bool IsCameraMainNull { get { return Camera.main == null; } }

		public long FrameCount { get; private set; } // Should be good for 4.8 billion years at 60 FPS!
		/// <summary>
		/// Has the camera moved since the last frame?
		/// </summary>
		/// <value><c>true</c> if camera has moved; otherwise, <c>false</c>.</value>
		public bool CameraHasMoved { get; private set; }
		public Vector3 CameraPosition { get { return IsCameraMainNull ? Vector3.zero : Camera.main.transform.position; } }
		public Vector3 CameraForward { get { return IsCameraMainNull ? Vector3.forward : Camera.main.transform.forward; } }
		public Vector3 CameraUp { get { return IsCameraMainNull ? Vector3.up : Camera.main.transform.up; } }
		public Quaternion CameraRotation { get { return IsCameraMainNull ? Quaternion.identity : Camera.main.transform.rotation; } }
		public Ray CameraViewportPointToRay(Vector3 pos) { return IsCameraMainNull ? new Ray(Vector3.zero, Vector3.forward) : Camera.main.ViewportPointToRay(pos); }
		
		public Camera Camera { get { return IsCameraMainNull ? null : Camera.main; } }

		public int InterfaceScale { get; private set; }
		#endregion
	}
}
