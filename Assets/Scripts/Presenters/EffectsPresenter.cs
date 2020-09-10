using System.Collections.Generic;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Presenters;

namespace Lunra.Hothouse.Presenters
{
	public class EffectsPresenter : Presenter<EffectsView>
	{
		GameModel game;
		EffectsModel effects;

		List<string> effectIdsPlayedThisFrame = new List<string>();

		public EffectsPresenter(
			GameModel game
		)
		{
			this.game = game;
			effects = game.Effects;
			
			game.SimulationInitialize += OnGameSimulationInitialize;
			game.SimulationUpdate += OnGameSimulationUpdate;

			effects.IsEnabled.Changed += OnFloraEffectsIsEnabled;
		}

		protected override void Deconstruct()
		{
			game.SimulationInitialize -= OnGameSimulationInitialize;
			game.SimulationUpdate -= OnGameSimulationUpdate;
			
			effects.IsEnabled.Changed -= OnFloraEffectsIsEnabled;
		}
		
		void Show()
		{
			if (View.Visible) return;
			
			View.Cleanup();
			
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
			OnFloraEffectsIsEnabled(effects.IsEnabled.Value);
		}
		
		void OnGameSimulationUpdate()
		{
			if (View.NotVisible) return;

			effectIdsPlayedThisFrame.Clear();
			
			int budgetRemaining = 10;

			while (0 < budgetRemaining)
			{
				if (!effects.Queued.TryPeek(out var peekResult)) break;
				if (effectIdsPlayedThisFrame.Contains(peekResult.Id)) break;

				var request = effects.Queued.Dequeue();
				
				effectIdsPlayedThisFrame.Add(request.Id);
				View.PlayEffect(request.Position, request.Id);
				budgetRemaining--;
			}
		}
		#endregion
		
		#region FloraEffectsModel Events
		void OnFloraEffectsIsEnabled(bool isEnabled)
		{
			if (isEnabled) Show();
			else Close();
		}
		#endregion
	}
}