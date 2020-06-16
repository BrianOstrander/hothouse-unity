using System;
using Lunra.NumberDemon;

namespace Lunra.Hothouse.Models
{
	public struct RoomResolverRequest
	{
		public static RoomResolverRequest Default(
			Demon generator,
			RoomPoolModel.ActivateRoom activateRoom,
			DoorPoolModel.ActivateDoor activateDoor
		)
		{
			return new RoomResolverRequest(
				generator,
				10,
				20,
				2,
				3,
				activateRoom,
				activateDoor
			);
		}

		public Demon Generator;
		public int RoomCountMinimum;
		public int RoomCountMaximum;
		public int SpawnDoorCountRequired;
		public int ExitDistanceMinimum;

		public RoomPoolModel.ActivateRoom ActivateRoom;
		public DoorPoolModel.ActivateDoor ActivateDoor;
		
		public DateTime BeginTime;
		public int RoomCountTarget;

		public RoomResolverRequest(
			Demon generator,
			int roomCountMinimum,
			int roomCountMaximum,
			int spawnDoorCountRequired,
			int exitDistanceMinimum,
			RoomPoolModel.ActivateRoom activateRoom,
			DoorPoolModel.ActivateDoor activateDoor
		)
		{
			Generator = generator;
			RoomCountMinimum = roomCountMinimum;
			RoomCountMaximum = roomCountMaximum;
			SpawnDoorCountRequired = spawnDoorCountRequired;
			ExitDistanceMinimum = exitDistanceMinimum;
			ActivateRoom = activateRoom;
			ActivateDoor = activateDoor;
			
			BeginTime = DateTime.Now;
			RoomCountTarget = Generator.GetNextInteger(RoomCountMinimum, RoomCountMaximum);
		}
	}
}