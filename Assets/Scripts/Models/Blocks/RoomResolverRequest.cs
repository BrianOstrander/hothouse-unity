using System;
using System.Collections.Generic;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public struct RoomResolverRequest
	{
		public static class Defaults
		{
			public static RoomResolverRequest Tiny(
				Demon generator,
				RoomPoolModel.ActivateRoom activateRoom,
				DoorPoolModel.ActivateDoor activateDoor
			)
			{
				return new RoomResolverRequest(
					generator,
					4,
					5,
					2,
					activateRoom,
					activateDoor
				);
			}
			
			public static RoomResolverRequest Small(
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
					activateRoom,
					activateDoor
				);
			}
			
			public static RoomResolverRequest Medium(
				Demon generator,
				RoomPoolModel.ActivateRoom activateRoom,
				DoorPoolModel.ActivateDoor activateDoor
			)
			{
				return new RoomResolverRequest(
					generator,
					20,
					40,
					2,
					activateRoom,
					activateDoor
				);
			}
		
			public static RoomResolverRequest Large(
				Demon generator,
				RoomPoolModel.ActivateRoom activateRoom,
				DoorPoolModel.ActivateDoor activateDoor
			)
			{
				return new RoomResolverRequest(
					generator,
					100,
					200,
					2,
					activateRoom,
					activateDoor
				);
			}	
		}

		public Demon Generator;
		public int RoomCountMinimum;
		public int RoomCountMaximum;
		public int SpawnDoorCountRequired;

		public RoomPoolModel.ActivateRoom ActivateRoom;
		public DoorPoolModel.ActivateDoor ActivateDoor;
		
		public int RoomCountTarget;

		public RoomResolverRequest(
			Demon generator,
			int roomCountMinimum,
			int roomCountMaximum,
			int spawnDoorCountRequired,
			RoomPoolModel.ActivateRoom activateRoom,
			DoorPoolModel.ActivateDoor activateDoor
		)
		{
			Generator = generator;
			RoomCountMinimum = roomCountMinimum;
			RoomCountMaximum = roomCountMaximum;
			SpawnDoorCountRequired = spawnDoorCountRequired;
			
			ActivateRoom = activateRoom;
			ActivateDoor = activateDoor;
			
			RoomCountTarget = Generator.GetNextInteger(RoomCountMinimum, RoomCountMaximum);
		}
	}
}