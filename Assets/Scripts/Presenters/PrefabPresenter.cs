using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;

namespace Lunra.WildVacuum.Presenters
{
	public class PrefabPresenter<V, M> : Presenter<V>
		where V : PrefabView
		where M : PrefabModel
	{
		protected GameModel Game;
		protected M Model;

		public PrefabPresenter(
			GameModel game,
			M model
		) : base(
			App.V.Get<V>(v => v.PrefabId == model.PrefabId.Value)
		)
		{
			Game = game;
			Model = model;

			Game.SimulationInitialize += OnGameSimulationInitialize;
			
			Model.IsEnabled.Changed += OnPrefabIsEnabled;
		}

		protected override void OnUnBind()
		{
			Game.SimulationInitialize -= OnGameSimulationInitialize;
			
			Model.IsEnabled.Changed -= OnPrefabIsEnabled;
		}
		
		protected virtual void Show()
		{
			if (View.Visible) return;
			
			View.Reset();

			OnShow();
			
			ShowView(instant: true);

			View.RootTransform.position = Model.Position.Value;
			View.RootTransform.rotation = Model.Rotation.Value;
		}

		protected virtual void Close()
		{
			if (View.NotVisible) return;
			
			OnClose();
			CloseView(true);
		}
		
		#region Events
		protected virtual void OnShow()
		{
			
		}
		
		protected virtual void OnClose()
		{
			
		}
		#endregion
		
		#region GameModel Events
		protected virtual void OnGameSimulationInitialize()
		{
			OnPrefabIsEnabled(Model.IsEnabled.Value);
		}
		#endregion
		
		#region PrefabModel Events
		protected virtual void OnPrefabIsEnabled(bool enabled)
		{
			if (enabled) Show();
			else Close();
		}
		#endregion
	}
}