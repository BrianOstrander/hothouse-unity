using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class RoomPoolModel : BasePrefabPoolModel<RoomModel>
	{
		public delegate RoomModel ActivateRoom(
			string id,
			string prefabId,
			Vector3 position,
			Quaternion rotation
		);
		
		public override void Initialize(GameModel game)
		{
			Initialize(
				game,
				model => new RoomPresenter(game, model)	
			);
		}

		public RoomModel Activate(
			string id,
			string prefabId,
			Vector3 position,
			Quaternion rotation
		)
		{
			return Activate(
				prefabId,
				id,
				position,
				rotation,
				m =>
				{
					m.Id.Value = id;
					
					m.IsSpawn.Value = false;
					m.SpawnDistance.Value = int.MaxValue;
					m.IsRevealed.Value = false;
					m.RevealDistance.Value = int.MaxValue;
					m.AdjacentRoomIds.Value = (new Dictionary<string, bool>()).ToReadonlyDictionary();
				}
			);
		}
	}
}