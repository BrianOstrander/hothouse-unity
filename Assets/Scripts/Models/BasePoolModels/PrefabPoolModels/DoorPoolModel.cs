using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models.AgentModels;
using Lunra.Hothouse.Presenters;
using Lunra.Hothouse.Views;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class DoorPoolModel : BasePrefabPoolModel<DoorPrefabModel>
	{
		static readonly string[] ValidPrefabIds = new[]
		{
			"default"
		};

		GameModel game;
		
		public override void Initialize(GameModel game)
		{
			this.game = game;
			Initialize(
				model => new DoorPrefabPresenter(game, model)	
			);
		}

		public DoorPrefabModel Activate(
			string roomId0,
			string roomId1,
			Vector3 position,
			Quaternion rotation
		)
		{
			return Activate(
				ValidPrefabIds.Random(),
				roomId0,
				position,
				rotation,
				m =>
				{
					m.RoomConnection.Value = new DoorPrefabModel.Connection(roomId0, roomId1);
				}
			);
		}
	}
}