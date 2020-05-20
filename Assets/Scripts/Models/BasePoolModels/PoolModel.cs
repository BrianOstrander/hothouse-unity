using System;

namespace Lunra.Hothouse.Models
{
	public class PoolModel<M> : BasePoolModel<M>
		where M : PooledModel, new()
	{
		public new void Initialize(Action<M> instantiatePresenter)
		{
			base.Initialize(instantiatePresenter);
		}

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