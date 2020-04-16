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

            game.WorldCamera.Enabled.Changed += OnWorldCameraEnabled;
            
            OnWorldCameraEnabled(game.WorldCamera.Enabled.Value);
        }

        protected override void OnUnBind()
        {
            game.WorldCamera.Enabled.Changed -= OnWorldCameraEnabled;
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
        void OnWorldCameraEnabled(bool enabled)
        {
            if (enabled) Show();
            else Close();
        }
        #endregion
    }
}