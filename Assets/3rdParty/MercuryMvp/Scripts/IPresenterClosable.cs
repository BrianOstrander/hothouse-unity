using UnityEngine;

namespace LunraGames.SubLight
{
	public interface IPresenterShow : IPresenter
	{
		void Show();
	}

	public interface IPresenterCloseShow : IPresenterShow
	{
		void Close();
	}

	public interface IPresenterShowOptions : IPresenter
	{
		void Show(Transform parent = null, bool instant = false);
	}

	public interface IPresenterCloseShowOptions : IPresenterShowOptions
	{
		void Close(bool instant = false);
	}
}