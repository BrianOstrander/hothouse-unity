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
	public class DoorPoolModel : BasePrefabPoolModel<DoorModel>
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
				model => new DoorPresenter(game, model)	
			);
		}

		public DoorModel Activate(
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
					m.RoomConnection.Value = new DoorModel.Connection(roomId0, roomId1);
				}
			);
		}
	}
}