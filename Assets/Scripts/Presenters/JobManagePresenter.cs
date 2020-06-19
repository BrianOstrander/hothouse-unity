using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Presenters;

namespace Lunra.Hothouse.Presenters
{
	public class JobManagePresenter : Presenter<JobManageView>
	{
		GameModel game;
		JobManageModel jobManage;

		public JobManagePresenter(GameModel game)
		{
			this.game = game;
			jobManage = game.JobManage;
			
			Show();
		}

		protected override void UnBind()
		{
			
		}

		void Show()
		{
			if (View.Visible) return;
			
			View.Reset();

			ShowView(instant: true);
		}
		
		#region GameModel Events
		void OnGameSimulationUpdate()
		{
			
		}
		#endregion
		
		// void Update
	}
}