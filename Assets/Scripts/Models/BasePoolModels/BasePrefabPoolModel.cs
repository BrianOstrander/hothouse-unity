using System;
using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class BasePrefabPoolModel<M> : BasePoolModel<M>
		where M : PrefabModel, new()
	{
		protected M Activate(
			string prefabId,
			string roomId,
			Vector3 position,
			Quaternion rotation
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
					OnActivate(m);
				},
				m =>
				{
					if (m.PrefabId.Value != prefabId) return false;
					return OnPredicate(m);
				}
			);
		}

		protected virtual void OnActivate(M model) { }

		protected virtual bool OnPredicate(Model model) => true;
	}
}