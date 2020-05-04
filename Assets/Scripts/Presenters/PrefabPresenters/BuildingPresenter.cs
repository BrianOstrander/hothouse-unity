using System.Linq;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;

namespace Lunra.WildVacuum.Presenters
{
	public abstract class BuildingPresenter<V, M> : PrefabPresenter<V, M>
		where V : BuildingView
		where M : BuildingModel
	{
		protected BuildingPresenter(GameModel game, M prefab) : base(game, prefab)
		{
			
		}

		#region PrefabModel Events
		protected override void OnPrefabIsEnabled(bool enabled)
		{
			base.OnPrefabIsEnabled(enabled);

			if (!enabled || Prefab.Entrances.Value.Any()) return;

			Prefab.Entrances.Value = View.Entrances.Select(e => new BuildingModel.Entrance(e, BuildingModel.Entrance.States.Available)).ToArray();
		}
		#endregion
	}
}