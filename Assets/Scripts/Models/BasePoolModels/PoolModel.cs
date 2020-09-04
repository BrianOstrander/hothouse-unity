using System;

namespace Lunra.Hothouse.Models
{
	public class PoolModel<M> : BasePoolModel<M>
		where M : PooledModel, new()
	{
		public new void Initialize(GameModel game, Action<M> instantiatePresenter) => base.Initialize(game, instantiatePresenter);

		public new M Activate(
			Action<M> initialize = null,
			Func<M, bool> predicate = null
		)
		{
			return base.Activate(initialize, predicate);
		}

		public new void InActivate(params M[] models)
		{
			base.InActivate(models);
		}
	}
}