using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using Lunra.WildVacuum.Services;
using Lunra.WildVacuum.Views;

namespace Lunra.WildVacuum.Presenters
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
		}

		protected override void UnBind()
		{
			game.Dwellers.All.Changed -= OnDwellersAll;
		}

		void Show()
		{
			if (View.Visible) return;
			
			View.Reset();

			View.Description = "All your dwellers died!";
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
			App.S.RequestState(
				new MainMenuPayload
				{
					Preferences = preferences
				}
			);
		}
		#endregion
		
		#region GameModel Events
		void OnDwellersAll(PoolModel<DwellerModel>.Reservoir all)
		{
			if (all.Active.Any()) return;

			Show();
		}
		#endregion
	}
}