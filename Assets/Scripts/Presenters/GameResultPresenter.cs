using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using Lunra.Hothouse.Services;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;

namespace Lunra.Hothouse.Presenters
{
	public class GameResultPresenter : Presenter<GameResultView>
	{
		GameModel game;
		PreferencesModel preferences;

		public GameResultPresenter(
			GameModel game,
			PreferencesModel preferences
		)
		{
			this.game = game;
			this.preferences = preferences;

			game.Dwellers.All.Changed += OnDwellersAll;
			game.LastLightUpdate.Changed += OnLastLightUpdate;
		}

		protected override void UnBind()
		{
			game.Dwellers.All.Changed -= OnDwellersAll;
			game.LastLightUpdate.Changed -= OnLastLightUpdate;
		}

		void Show(string reason)
		{
			if (View.Visible) return;

			game.Toolbar.IsEnabled.Value = false;
			
			View.Reset();

			View.Description = reason;
			View.ButtonDescription = "Restart";

			View.Click += OnViewClick;
			
			ShowView(instant: true);
		}

		void Close()
		{
			if (View.NotVisible) return;
			
			CloseView(true);
		}
		
		#region View Events
		void OnViewClick()
		{
			Close();
			game.GameResult.Value = new GameResult(GameResult.States.Failure, "todo record this");
		}
		#endregion
		
		#region GameModel Events
		void OnDwellersAll(GenericPrefabPoolModel<DwellerModel>.Reservoir all)
		{
			if (all.Active.Any()) return;

			Show("All your dwellers died!");
		}

		void OnLastLightUpdate(LightDelta lightUpdate)
		{
			if (lightUpdate.State != LightDelta.States.Calculated) return;
			if (game.GetLightsActive().Any(l => l.IsLightActive())) return;
			
			Show("Plunged into darkness, your fires went out!");
		}
		#endregion
	}
}