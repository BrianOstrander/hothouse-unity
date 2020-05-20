using System;
using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public abstract class BasePrefabPoolModel<M> : BasePoolModel<M>
		where M : PrefabModel, new()
	{
		public abstract void Initialize(GameModel game);
		
		protected M Activate(
			string prefabId,
			string roomId,
			Vector3 position,
			Quaternion rotation,
			Action<M> initialize = null,
			Func<M, bool> predicate = null
		)
		{
			if (string.IsNullOrEmpty(prefabId)) throw new ArgumentException("Cannot be null or empty", nameof(prefabId));
			if (string.IsNullOrEmpty(roomId)) throw new ArgumentException("Cannot be null or empty", nameof(roomId));
			
			return base.Activate(
				m =>
				{
					m.PrefabId.Value = prefabId;
					m.RoomId.Value = roomId;
					m.Position.Value = position;
					m.Rotation.Value = rotation;
					initialize?.Invoke(m);
				},
				m =>
				{
					if (m.PrefabId.Value != prefabId) return false;
					return predicate?.Invoke(m) ?? true;
				}
			);
		}
	}
}