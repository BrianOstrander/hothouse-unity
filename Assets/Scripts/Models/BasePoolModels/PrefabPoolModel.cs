using System;

namespace Lunra.Hothouse.Models
{
	public class PrefabPoolModel<M> : BasePoolModel<M>
		where M : PrefabModel, new()
	{
		public new void Initialize(Action<M> instantiatePresenter)
		{
			base.Initialize(instantiatePresenter);
		}

		public M Activate(
			string prefabId,
			Action<M> initialize = null,
			Func<M, bool> predicate = null
		)
		{
			if (string.IsNullOrEmpty(prefabId)) throw new ArgumentException("Cannot be null or empty", nameof(prefabId));
			
			return base.Activate(
				m =>
				{
					m.PrefabId.Value = prefabId;
					initialize?.Invoke(m);
				},
				m =>
				{
					if (m.PrefabId.Value != prefabId) return false;
					return predicate?.Invoke(m) ?? true;
				}
			);
		}

		public new void InActivate(params M[] models)
		{
			base.InActivate(models);
		}
	}
}