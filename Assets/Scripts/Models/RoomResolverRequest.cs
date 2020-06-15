using System;

namespace Lunra.Hothouse.Models
{
	public struct RoomResolverRequest
	{
		public static RoomResolverRequest Default(
			int seed,
			RoomPoolModel.ActivateRoom activateRoom,
			DoorPoolModel.ActivateDoor activateDoor,
			Action<RoomResolverResult> done
		)
		{
			return new RoomResolverRequest(
				seed,
				10,
				20,
				2,
				activateRoom,
				activateDoor,
				done
			);
		}
		
		public int Seed;
		public int RoomCountMinimum;
		public int RoomCountMaximum;
		public int SpawnDoorCountRequired;

		public RoomPoolModel.ActivateRoom ActivateRoom;
		public DoorPoolModel.ActivateDoor ActivateDoor;

		public Action<RoomResolverResult> Done;
		
		public RoomResolverRequest(
			int seed,
			int roomCountMinimum,
			int roomCountMaximum,
			int spawnDoorCountRequired,
			RoomPoolModel.ActivateRoom activateRoom,
			DoorPoolModel.ActivateDoor activateDoor,
			Action<RoomResolverResult> done
		)
		{
			Seed = seed;
			RoomCountMinimum = roomCountMinimum;
			RoomCountMaximum = roomCountMaximum;
			SpawnDoorCountRequired = spawnDoorCountRequired; 
			ActivateRoom = activateRoom;
			ActivateDoor = activateDoor;
			Done = done;
		}
	}
}