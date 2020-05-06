using System.Linq;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;

namespace Lunra.WildVacuum.Presenters
{
	public abstract class BuildingPresenter<M, V> : PrefabPresenter<M, V>
		where M : BuildingModel
		where V : BuildingView
	{
		protected BuildingPresenter(GameModel game, M model) : base(game, model) { }

		#region View Events
		protected override void OnShow()
		{
			Model.Entrances.Value = View.Entrances.Select(e => new BuildingModel.Entrance(e, BuildingModel.Entrance.States.Available)).ToArray();
		}
		#endregion
	}
}