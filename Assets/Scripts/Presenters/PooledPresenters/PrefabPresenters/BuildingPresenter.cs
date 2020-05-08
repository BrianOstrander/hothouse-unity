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
		protected override void OnViewShown()
		{
			Model.Entrances.Value = View.Entrances.Select(e => new Entrance(e, Entrance.States.Available)).ToArray();
		}
		#endregion
	}
}