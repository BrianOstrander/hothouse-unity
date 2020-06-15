using System;
using System.Collections.Generic;
using Lunra.Hothouse.Views;

namespace Lunra.Hothouse.Models
{
	public struct RoomResolverRequest
	{
		public int Seed;
		public int RoomCountMinimum;
		public int RoomCountMaximum;

		public RoomPoolModel.ActivateRoom ActivateRoom;
		public DoorPoolModel.ActivateDoor ActivateDoor;

		public Action<RoomResolverResult> Done;
		
		public RoomResolverRequest(
			int seed,
			int roomCountMinimum,
			int roomCountMaximum,
			RoomPoolModel.ActivateRoom activateRoom,
			DoorPoolModel.ActivateDoor activateDoor,
			Action<RoomResolverResult> done
		)
		{
			Seed = seed;
			RoomCountMinimum = roomCountMinimum;
			RoomCountMaximum = roomCountMaximum;
			ActivateRoom = activateRoom;
			ActivateDoor = activateDoor;
			Done = done;
		}
	}
}