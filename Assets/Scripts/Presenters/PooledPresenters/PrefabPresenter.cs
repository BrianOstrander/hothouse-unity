using Lunra.StyxMvp;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;

namespace Lunra.WildVacuum.Presenters
{ 
	public class PrefabPresenter<M, V> : PooledPresenter<M, V>
		where M : PrefabModel
		where V : PrefabView
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