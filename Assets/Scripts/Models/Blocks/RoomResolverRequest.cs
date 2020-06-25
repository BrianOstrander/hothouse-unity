using System;
using System.Collections.Generic;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public struct RoomResolverRequest
	{
		public struct FloraConstraint
		{
			public FloraSpecies Species;
			public int CountPerRoomMinimum;
			public int CountPerRoomMaximum;
			public float SpawnDistanceNormalizedMinimum;
			public int CountPerClusterMinimum;
			public int CountPerClusterMaximum;
			
			public FloraConstraint(
				FloraSpecies species,
				int countPerRoomMinimum,
				int countPerRoomMaximum,
				float spawnDistanceNormalizedMinimum,
				int countPerClusterMinimum,
				int countPerClusterMaximum
			)
			{
				Species = species;
				CountPerRoomMinimum = countPerRoomMinimum;
				CountPerRoomMaximum = countPerRoomMaximum;
				SpawnDistanceNormalizedMinimum = spawnDistanceNormalizedMinimum;
				CountPerClusterMinimum = countPerClusterMinimum;
				CountPerClusterMaximum = countPerClusterMaximum;
			}
		}
		
		static readonly FloraConstraint[] DefaultFloraConstraints = 
		{
			new FloraConstraint(
				FloraSpecies.Grass,
				0,
				3,
				0f,
				4,
				10
			),
			new FloraConstraint(
				FloraSpecies.Wheat,
				0,
				6,
				0f,
				3,
				6
			),
			new FloraConstraint(
				FloraSpecies.Shroom,
				0,
				1,
				0.5f,
				1,
				2
			),
			new FloraConstraint(
				FloraSpecies.SeekerSpawner,
				1,
				1,
				0f,
				1,
				2
			)
		};
		
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
				DefaultFloraConstraints,
				activateRoom,
				activateDoor
			);
		}
		
		public static RoomResolverRequest DefaultLarge(
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
				DefaultFloraConstraints,
				activateRoom,
				activateDoor
			);
		}

		public Demon Generator;
		public int RoomCountMinimum;
		public int RoomCountMaximum;
		public int SpawnDoorCountRequired;

		public FloraConstraint[] FloraConstraints;

		public RoomPoolModel.ActivateRoom ActivateRoom;
		public DoorPoolModel.ActivateDoor ActivateDoor;
		
		public int RoomCountTarget;

		public RoomResolverRequest(
			Demon generator,
			int roomCountMinimum,
			int roomCountMaximum,
			int spawnDoorCountRequired,
			FloraConstraint[] floraConstraints,
			RoomPoolModel.ActivateRoom activateRoom,
			DoorPoolModel.ActivateDoor activateDoor
		)
		{
			Generator = generator;
			RoomCountMinimum = roomCountMinimum;
			RoomCountMaximum = roomCountMaximum;
			SpawnDoorCountRequired = spawnDoorCountRequired;
			FloraConstraints = floraConstraints;
			ActivateRoom = activateRoom;
			ActivateDoor = activateDoor;
			
			RoomCountTarget = Generator.GetNextInteger(RoomCountMinimum, RoomCountMaximum);
		}
	}
}