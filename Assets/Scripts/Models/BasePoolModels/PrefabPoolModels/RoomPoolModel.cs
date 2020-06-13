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
		
		GameModel game;
		
		public override void Initialize(GameModel game)
		{
			this.game = game;
			Initialize(
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
				m => m.Id.Value = id
			);
		}
	}
}