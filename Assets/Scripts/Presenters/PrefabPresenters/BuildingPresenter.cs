using System.Linq;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;

namespace Lunra.WildVacuum.Presenters
{
	public abstract class BuildingPresenter<V, M> : PrefabPresenter<V, M>
		where V : BuildingView
		where M : BuildingModel
	{
		protected BuildingPresenter(GameModel game, M model) : base(game, model)
		{
			
		}

		#region PrefabModel Events
		protected override void OnPrefabIsEnabled(bool enabled)
		{
			base.OnPrefabIsEnabled(enabled);

			if (!enabled || Model.Entrances.Value.Any()) return;

			Model.Entrances.Value = View.Entrances.Select(e => new BuildingModel.Entrance(e, BuildingModel.Entrance.States.Available)).ToArray();
		}
		#endregion
	}
}