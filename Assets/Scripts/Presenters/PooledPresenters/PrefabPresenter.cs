using Lunra.StyxMvp;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;

namespace Lunra.WildVacuum.Presenters
{
	public class PrefabPresenter<V, M> : PooledPresenter<V, M>
		where V : PrefabView
		where M : PrefabModel
	{
		public PrefabPresenter(
			GameModel game,
			M model
		) : base(
			game,
			model,
			App.V.Get<V>(v => v.PrefabId == model.PrefabId.Value)
		) { }
	}
}