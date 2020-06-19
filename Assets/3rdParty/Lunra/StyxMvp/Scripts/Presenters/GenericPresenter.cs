using UnityEngine;

namespace Lunra.StyxMvp.Presenters
{
	public class GenericPresenter<V> : Presenter<V>
		where V : class, IView
	{
		public GenericPresenter(string layer = null)
		{
			if (!string.IsNullOrEmpty(layer)) View.SetLayer(layer);
		}

		public void Show(Transform parent = null, bool instant = false)
		{
			if (View.Visible) return;

			View.Cleanup();

			ShowView(parent, instant);
		}

		public void Close(bool instant = false)
		{
			if (!View.Visible) return;

			CloseView(instant);
		}
		
		#region Events
		#endregion
	}
}