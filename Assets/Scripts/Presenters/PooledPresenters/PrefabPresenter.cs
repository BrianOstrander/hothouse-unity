using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;

namespace Lunra.Hothouse.Presenters
{ 
	public class PrefabPresenter<M, V> : PooledPresenter<M, V>
		where M : IPrefabModel
		where V : class, IPrefabView
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