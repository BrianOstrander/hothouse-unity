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

			Game.SimulationInitialize += OnGameSimulationInitialize;
			
			Prefab.IsEnabled.Changed += OnPrefabIsEnabled;
		}

		protected override void OnUnBind()
		{
			Game.SimulationInitialize -= OnGameSimulationInitialize;
			
			Prefab.IsEnabled.Changed -= OnPrefabIsEnabled;
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
		
		#region GameModel Events
		protected virtual void OnGameSimulationInitialize()
		{
			OnPrefabIsEnabled(Prefab.IsEnabled.Value);
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