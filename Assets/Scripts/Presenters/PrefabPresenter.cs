using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;

namespace Lunra.WildVacuum.Presenters
{
    public class PrefabPresenter<V, M> : Presenter<V>
        where M : PrefabModel
        where V : PrefabView
    {
        protected GameModel Game;
        protected M Prefab;

        public PrefabPresenter(
            GameModel game,
            M prefab
        ) : base(
            App.V.Get<V>(v => v.PrefabId == prefab.PrefabId.Value)
        )
        {
            Game = game;
            Prefab = prefab;

            prefab.Enabled.Changed += OnPrefabEnabled;
            
            OnPrefabEnabled(prefab.Enabled.Value);
        }

        protected override void OnUnBind()
        {
            Prefab.Enabled.Changed -= OnPrefabEnabled;
        }
        
        protected virtual void Show()
        {
            if (View.Visible) return;
            
            View.Reset();

            ShowView(instant: true);

            View.RootTransform.position = Prefab.Position.Value;
            View.RootTransform.rotation = Prefab.Rotation.Value;
        }

        protected virtual void Close()
        {
            if (View.NotVisible) return;
            
            CloseView(true);
        }
        
        #region PrefabModel Events
        protected virtual void OnPrefabEnabled(bool enabled)
        {
            if (enabled) Show();
            else Close();
        }
        #endregion
    }
}