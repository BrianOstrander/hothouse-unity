using System;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
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

		protected override void Deconstruct()
		{
			roomResolver.Initialize -= OnRoomResolverInitialize;
			roomResolver.Generate -= OnRoomResolverGenerate;
		}
		
		#region RoomResolverModel Events
		void OnRoomResolverInitialize(Action done)
		{
			View.Cleanup();
			
			ShowView(instant: true);

			foreach (var entry in App.V.GetPrefabs<RoomView>())
			{
				View.AddRoomDefinition(
					entry.View.PrefabId,
					entry.View.BoundaryColliders,
					entry.View.DoorDefinitions,
					entry.View.WallDefinitions,
					entry.View.PrefabTags
				);
			}
			
			foreach (var entry in App.V.GetPrefabs<DoorView>())
			{
				View.AddDoorDefinition(
					entry.View.PrefabId,
					entry.View.DoorDefinitions,
					entry.View.PrefabTags
				);
			}
			
			done();
		}
		
		void OnRoomResolverGenerate(
			RoomResolverRequest request,	
			Action<RoomResolverResult> done
		)
		{
			ShowView(instant: true);
			View.Generate(
				request,
				result => OnRoomResolverGenerateDone(result, done)
			);
		}

		void OnRoomResolverGenerateDone(
			RoomResolverResult result,
			Action<RoomResolverResult> done
		)
		{
			CloseView(true);
			done(result);
		}
		#endregion
	}
}