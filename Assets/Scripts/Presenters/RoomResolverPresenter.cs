using System;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.NumberDemon;
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
				10f
			);
			
			View.Generate(
				request,
				OnRoomResolverGenerateDone
			);
		}

		void OnRoomResolverGenerateDone(RoomResolverResult result)
		{
			
		}
		#endregion
	}
}