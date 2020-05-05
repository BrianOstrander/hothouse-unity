using UnityEngine;

namespace Lunra.StyxMvp
{
	public abstract class ViewAnimation : ScriptableObject
	{
		public virtual void OnPrepare(IView view){}
		public virtual void OnShowing(IView view, float scalar) {}
		public virtual void OnShown(IView view) {}
		public virtual void OnIdle(IView view) {}
		public virtual void OnLateIdle(IView view) {}
		public virtual void OnPrepareClose(IView view) { }
		public virtual void OnClosing(IView view, float scalar) {}
		public virtual void OnClosed(IView view) {}
		public virtual void OnConstant(IView view) {}
		public virtual void OnLateConstant(IView view) {}

		long lastFrame;
		long lastFrameLate;

		/// <summary>
		/// Triggers events that should only be run once per frame of this
		/// animation, ever, across all views. Good for limiting expensive
		/// operations to once per frame.
		/// </summary>
		/// <param name="view">View.</param>
		/// <param name="delta">Delta.</param>
		public void ConstantOnce(IView view)
		{
			if (lastFrame != App.V.FrameCount)
			{
				lastFrame = App.V.FrameCount;
				OnConstantOnce(view);
			}
		}

		/// <summary>
		/// Triggers events that should only be run once per frame of this
		/// animation, ever, across all views. Good for limiting expensive
		/// operations to once per frame.
		/// </summary>
		/// <param name="view">View.</param>
		/// <param name="delta">Delta.</param>
		public void LateConstantOnce(IView view)
		{
			if (lastFrameLate != App.V.FrameCount)
			{
				lastFrameLate = App.V.FrameCount;
				OnLateConstantOnce(view);
			}
		}

		public virtual void OnConstantOnce(IView view) {}
		public virtual void OnLateConstantOnce(IView view) {}
	}
}