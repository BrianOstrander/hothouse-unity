using System;

using UnityEngine;

namespace Lunra.StyxMvp.Presenters
{
	public interface IPresenter
	{
		Type ViewInterface { get; }
		bool IsDeconstructed { get; }
	}
	
	/// <summary>
	/// Base Presenter, defining its view and model interface.
	/// </summary>
	public abstract class Presenter<V> : IPresenter
		where V : class, IView
	{
		/// <summary>
		/// Gets the view using the interface defined in the presenter.
		/// </summary>
		/// <value>The view.</value>
		public V View { get; private set; }
		/// <summary>
		/// Gets the view interface's type for this presenter.
		/// </summary>
		/// <value>The view interface type.</value>
		public Type ViewInterface => typeof(V);

		public bool IsDeconstructed { private set; get; }

		public Presenter() : this(App.V.Get<V>()) {}

		public Presenter(V view)
		{
			Register();
			SetView (view);
		}

		protected void Register()
		{
			App.P.Register(
				this,
				() => View.TransitionState,
				CloseView,
				TriggerDeconstruct
			);
		}

		protected void SetView(V view)
		{
			if (view == null) 
			{
				Debug.LogError("Unable to get an instance of a view for " + GetType());
				return;
			}
			
			View = view;
			View.Cleanup();
		}

		protected virtual Transform DefaultAnchor => null;

		void TriggerDeconstruct()
		{
			if (IsDeconstructed) return;
			IsDeconstructed = true;
			App.V.Pool(View);
			Deconstruct();
		}

		protected virtual void Deconstruct() {}

		protected virtual void ShowView(Transform parent = null, bool instant = false)
		{
			App.V.Show(View, instant, parent ? parent : DefaultAnchor);
		}

		protected virtual void CloseView(bool instant = false)
		{
			App.V.Close(View, instant);
		}
	}
}