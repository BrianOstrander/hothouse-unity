using System.Linq;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
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
			game.GameResult.Changed += OnGameResult;
		}

		protected override void UnBind()
		{
			game.Dwellers.All.Changed -= OnDwellersAll;
			game.LastLightUpdate.Changed -= OnLastLightUpdate;
			game.GameResult.Changed -= OnGameResult;
		}

		#region View Events
		void OnViewClick()
		{
			if (View.NotVisible) return;
			
			CloseView(true);
			game.GameResult.Value = game.GameResult.Value.New(GameResult.States.Failure);
		}
		#endregion
		
		#region GameModel Events
		void OnDwellersAll(GenericPrefabPoolModel<DwellerModel>.Reservoir all)
		{
			if (!game.IsSimulating) return;
			if (all.Active.Any()) return;

			game.GameResult.Value = new GameResult(
				GameResult.States.Displaying,
				"All your dwellers died!",
				game.SimulationTime.Value
			);
		}

		void OnLastLightUpdate(LightDelta lightUpdate)
		{
			if (!game.IsSimulating) return;
			if (lightUpdate.State != LightDelta.States.Calculated) return;
			if (game.Query.Any<ILightModel>(m => m.Light.IsLightActive())) return;

			game.GameResult.Value = new GameResult(
				GameResult.States.Displaying,
				"Plunged into darkness, your fires went out!",
				game.SimulationTime.Value
			);
		}
		
		void OnGameResult(GameResult result)
		{
			if (result.State != GameResult.States.Displaying) return;
			if (View.Visible) return;

			game.Toolbar.IsEnabled.Value = false;
			
			View.Cleanup();

			View.Description = result.Reason + "\n" + result.TimeSurvived;
			View.ButtonDescription = "Restart";

			View.Click += OnViewClick;
			
			ShowView(instant: true);
		}
		#endregion
	}
}