using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Presenters;

namespace Lunra.Hothouse.Presenters
{
	public class WorldCameraPresenter : Presenter<WorldCameraView>
	{
		GameModel game;

		public WorldCameraPresenter(GameModel game)
		{
			this.game = game;

			game.SimulationInitialize += OnGameSimulationInitialize;
			
			game.WorldCamera.IsEnabled.Changed += OnWorldCameraIsEnabled;
			
			game.WorldCamera.CameraInstance.Value = View.CameraInstance;
			game.Input.Camera.Value = View.CameraInstance;
		}

		protected override void UnBind()
		{
			game.SimulationInitialize -= OnGameSimulationInitialize;
			
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
		
		#region GameModel Events
		void OnGameSimulationInitialize()
		{
			OnWorldCameraIsEnabled(game.WorldCamera.IsEnabled.Value);
		}
		#endregion
		
		#region WorldCameraModel Events
		void OnWorldCameraIsEnabled(bool enabled)
		{
			if (enabled) Show();
			else Close();
		}
		#endregion
	}
}