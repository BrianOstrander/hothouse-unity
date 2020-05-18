using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;

namespace Lunra.Hothouse.Presenters
{
	public class RoomPrefabPresenter : PrefabPresenter<RoomPrefabModel, RoomPrefabView>
	{
		public RoomPrefabPresenter(GameModel game, RoomPrefabModel model) : base(game, model) { }

		protected override void Bind()
		{
			Game.SimulationTime.Changed += GameSimulationTime;

			base.Bind();
		}

		protected override void UnBind()
		{
			Game.SimulationTime.Changed -= GameSimulationTime;

			base.UnBind();
		}

		#region Game Events
		void GameSimulationTime(DayTime dayTime)
		{
			if (View.NotVisible) return;

			View.TimeOfDay = dayTime.Time;
		}
		#endregion
	}
}