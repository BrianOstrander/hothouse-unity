using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;

namespace Lunra.Hothouse.Presenters
{ 
	public abstract class PrefabPresenter<M, V> : PooledPresenter<M, V>
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