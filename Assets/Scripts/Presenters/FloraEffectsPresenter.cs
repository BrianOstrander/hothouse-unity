using Lunra.StyxMvp.Presenters;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;
using UnityEngine;

namespace Lunra.WildVacuum.Presenters
{
	public class FloraEffectsPresenter : Presenter<FloraEffectsView>
	{
		GameModel game;
		FloraEffectsModel floraEffects;

		public FloraEffectsPresenter(
			GameModel game
		)
		{
			this.game = game;
			floraEffects = game.FloraEffects;
			
			game.SimulationInitialize += OnGameSimulationInitialized;

			floraEffects.Spawn += OnFloraEffectsSpawn;
			floraEffects.Chop += OnFloraEffectsChop;
		}

		protected override void OnUnBind()
		{
			game.SimulationInitialize -= OnGameSimulationInitialized;
			
			floraEffects.Spawn -= OnFloraEffectsSpawn;
			floraEffects.Chop -= OnFloraEffectsChop;
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
		void OnGameSimulationInitialized()
		{
			OnFloraEffectsIsEnabled(floraEffects.IsEnabled.Value);
		}
		#endregion
		
		#region FloraEffectsModel Events
		void OnFloraEffectsIsEnabled(bool isEnabled)
		{
			if (isEnabled) Show();
			else Close();
		}
		
		void OnFloraEffectsSpawn(FloraEffectsModel.Request request)
		{
			if (View.NotVisible) return;

			View.RootTransform.position = request.Position;
			View.Spawn();
		}

		void OnFloraEffectsChop(FloraEffectsModel.Request request)
		{
			if (View.NotVisible) return;

			View.RootTransform.position = request.Position;
			View.Chop();
		}
		#endregion
	}
}