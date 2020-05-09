using System;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Models;
using Lunra.StyxMvp.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
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

		protected override void UnBind()
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
		
		void OnGameSimulationUpdate()
		{
			if (View.NotVisible) return;

			DequeueEffect(floraEffects.SpawnQueue, View.PlaySpawn);
			DequeueEffect(floraEffects.HurtQueue, View.PlayHurt);
			DequeueEffect(floraEffects.DeathQueue, View.PlayDeath);
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