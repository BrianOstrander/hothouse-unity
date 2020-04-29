using System;
using Lunra.StyxMvp.Models;
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
			
			game.SimulationInitialize += OnGameSimulationInitialize;
			game.SimulationUpdate += OnGameSimulationUpdate;
		}

		protected override void OnUnBind()
		{
			game.SimulationInitialize -= OnGameSimulationInitialize;
			game.SimulationUpdate -= OnGameSimulationUpdate;
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
			OnFloraEffectsIsEnabled(floraEffects.IsEnabled.Value);
		}
		
		void OnGameSimulationUpdate(float delta)
		{
			if (View.NotVisible) return;

			DequeueEffect(floraEffects.SpawnQueue, View.PlaySpawn);
			DequeueEffect(floraEffects.ChopQueue, View.PlayChop);
		}
		#endregion
		
		#region FloraEffectsModel Events
		void OnFloraEffectsIsEnabled(bool isEnabled)
		{
			if (isEnabled) Show();
			else Close();
		}
		#endregion
		
		#region Utility
		void DequeueEffect(QueueProperty<FloraEffectsModel.Request> queue, Action<Vector3> play)
		{
			if (queue.TryDequeue(out var request)) play(request.Position);
		}
		#endregion
	}
}