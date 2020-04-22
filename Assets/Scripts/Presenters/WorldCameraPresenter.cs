using Lunra.StyxMvp.Presenters;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;

namespace Lunra.WildVacuum.Presenters
{
	public class WorldCameraPresenter : Presenter<WorldCameraView>
	{
		GameModel game;

		public WorldCameraPresenter(GameModel game)
		{
			this.game = game;

			game.WorldCamera.IsEnabled.Changed += OnWorldCameraIsEnabled;
			
			OnWorldCameraIsEnabled(game.WorldCamera.IsEnabled.Value);
		}

		protected override void OnUnBind()
		{
			game.WorldCamera.IsEnabled.Changed -= OnWorldCameraIsEnabled;
		}

		void Show()
		{
			if (View.Visible) return;
			
			View.Reset();
			
			ShowView(instant: true);
		}

		void Close()
		{
			if (View.NotVisible) return;
			
			CloseView(true);
		}
		
		#region WorldCameraModel Events
		void OnWorldCameraIsEnabled(bool enabled)
		{
			if (enabled) Show();
			else Close();
		}
		#endregion
	}
}