using System;
using System.Linq;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.NumberDemon;
using UnityEngine;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;

namespace Lunra.Hothouse.Presenters
{
	public class RoomResolverPresenter : Presenter<RoomResolverView>
	{
		GameModel game;
		RoomResolverModel roomResolver;
		
		public RoomResolverPresenter(GameModel game)
		{
			this.game = game;
			roomResolver = game.RoomResolver;

			roomResolver.Initialize += OnRoomResolverInitialize;
			roomResolver.Generate += OnRoomResolverGenerate;
		}

		protected override void UnBind()
		{
			roomResolver.Initialize -= OnRoomResolverInitialize;
			roomResolver.Generate -= OnRoomResolverGenerate;
		}
		
		#region RoomResolverModel Events
		void OnRoomResolverInitialize(Action done)
		{
			View.Reset();
			
			ShowView(instant: true);
			
			// var doorPrefabs = App.V.GetPrefabs<DoorView>();

			foreach (var room in App.V.GetPrefabs<RoomView>())
			{
				View.AddRoomDefinition(
					room.View.PrefabId,
					room.View.RoomColliders,
					room.View.DoorAnchors
				);
			}
			
			foreach (var room in App.V.GetPrefabs<DoorView>())
			{
				View.AddDoorDefinition(
					room.View.PrefabId,
					room.View.DoorAnchors
				);
			}
			
			done();
		}
		
		void OnRoomResolverGenerate(Action done)
		{
			var request = new RoomResolverRequest(
				DemonUtility.GetNextInteger(int.MinValue, int.MaxValue),
				10,
				20,
				game.Rooms.Activate,
				game.Doors.Activate,
				result =>
				{
					Debug.Log(result);
					if (game.Dwellers.AllActive.Any()) OnRoomResolverGenerateDone(done);
					else OnRoomResolverGenerateDwellers(done);
				}
			);
			
			View.Generate(request);
		}

		void OnRoomResolverGenerateDwellers(Action done)
		{
			var startingRoom = game.Rooms.FirstActive();

			startingRoom.IsRevealed.Value = true;
			
			var dweller0 = game.Dwellers.Activate(
				startingRoom.Id.Value,
				startingRoom.Transform.Position.Value
			);
			dweller0.Id.Value = "0";
			dweller0.Job.Value = Jobs.Clearer;
			
			var dweller1 = game.Dwellers.Activate(
				startingRoom.Id.Value,
				startingRoom.Transform.Position.Value + (Vector3.forward * 2f)
			);
			
			dweller1.Id.Value = "1";
			dweller1.Job.Value = Jobs.Construction;


			for (var i = 0; i < 4; i++)
			{
				var dweller = game.Dwellers.Activate(
					startingRoom.Id.Value,
					startingRoom.Transform.Position.Value + (Vector3.forward * 2f)
				);

				dweller.Id.Value = (2 + i).ToString();
				dweller.Job.Value = Jobs.Construction;	
			}
			
			var bonfire = game.Buildings.Activate(
				Buildings.Bonfire,
				startingRoom.Id.Value,
				startingRoom.Transform.Position.Value + (Vector3.right * 2f),
				Quaternion.identity,
				BuildingStates.Operating
			);
			
			var wagon = game.Buildings.Activate(
				Buildings.StartingWagon,
				startingRoom.Id.Value,
				startingRoom.Transform.Position.Value + (Vector3.left * 2f),
				Quaternion.identity * Quaternion.Euler(0f, 90f, 0f),
				BuildingStates.Operating
			);

			wagon.Inventory.Value += (Inventory.Types.Stalks, 999);
			
			game.WorldCamera.Transform.Position.Value = bonfire.Transform.Position.Value;

			game.WorldCamera.Transform.Rotation.Value = Quaternion.LookRotation(
				new Vector3(
					-1f,
					0f,
					-1f
				).normalized,
				Vector3.up
			);
			
			OnRoomResolverGenerateDone(done);
		}

		void OnRoomResolverGenerateDone(Action done)
		{
			CloseView(true);
			done();
		}
		#endregion
	}
}