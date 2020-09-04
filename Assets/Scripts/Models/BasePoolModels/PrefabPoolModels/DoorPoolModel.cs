using System.Linq;
using Lunra.Hothouse.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class DoorPoolModel : BasePrefabPoolModel<DoorModel>
	{
		public delegate DoorModel ActivateDoor(
			string id,
			string prefabId,
			string roomId0,
			string roomId1,
			Vector3 position,
			Quaternion rotation
		);

		public override void Initialize(GameModel game)
		{
			Initialize(
				game,
				model => new DoorPresenter(game, model)	
			);
		}

		public DoorModel Activate(
			string id,
			string prefabId,
			string roomId0,
			string roomId1,
			Vector3 position,
			Quaternion rotation
		)
		{
			return Activate(
				prefabId,
				roomId0,
				position,
				rotation,
				m =>
				{
					m.Id.Value = id;
					m.IsOpen.Value = false;
					m.Obligations.Reset();
					m.RoomConnection.Value = new DoorModel.Connection(roomId0, roomId1);
					m.LightSensitive.ConnectedRoomId.Value = roomId1;
					
					m.Enterable.Reset();
				}
			);
		}
	}
}