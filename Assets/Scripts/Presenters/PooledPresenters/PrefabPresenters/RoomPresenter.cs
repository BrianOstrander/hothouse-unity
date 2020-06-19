using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class RoomPresenter : PrefabPresenter<RoomModel, RoomView>
	{
		public RoomPresenter(GameModel game, RoomModel model) : base(game, model) { }

		protected override void Bind()
		{
			Game.SimulationTime.Changed += OnGameSimulationTime;

			Model.Boundary.Contains = OnRoomBoundaryContains;
			Model.Boundary.RandomPoint = OnRoomBoundaryRandomPoint;
			
			Model.IsRevealed.Changed += OnRoomIsRevealed;
			Model.RevealDistance.Changed += OnRoomRevealDistance;
			
			Model.UpdateConnection += OnRoomUpdateConnection;

			base.Bind();
		}

		protected override void UnBind()
		{
			Game.SimulationTime.Changed -= OnGameSimulationTime;

			Model.Boundary.Contains = null;
			Model.Boundary.RandomPoint = null;
			
			Model.IsRevealed.Changed -= OnRoomIsRevealed;
			Model.RevealDistance.Changed -= OnRoomRevealDistance;

			Model.UpdateConnection -= OnRoomUpdateConnection;

			base.UnBind();
		}
		
		protected override bool AutoShowCloseOnRoomReveal => false;
		
		#region View Events
		protected override void OnViewPrepare()
		{
			base.OnViewPrepare();

			OnRoomIsRevealed(Model.IsRevealed.Value);
		}
		#endregion

		#region PooledModel Events
		protected override bool CanShow() => Model.IsRevealed.Value;
		#endregion
		
		#region Game Events
		protected override void OnSimulationInitialized()
		{
			Model.AdjacentRoomIds.Value = Game.GetAdjacentRooms(Model.Id.Value).ToReadonlyDictionary();
		}

		void OnGameSimulationTime(DayTime dayTime)
		{
			if (View.NotVisible) return;

			View.TimeOfDay = dayTime.Time;
		}
		#endregion
		
		#region RoomModel Events
		void OnRoomIsRevealed(bool isRevealed)
		{
			if (isRevealed)
			{
				Model.RevealDistance.Value = 0;
				Show();
				View.IsRevealed = Model.IsRevealed.Value;
			}
			else Close();
		}

		void OnRoomRevealDistance(int revealDistance)
		{
			if (!Game.IsSimulating.Value) return; 
			
			foreach (var adjacentRoom in Model.AdjacentRoomIds.Value)
			{
				var room = Game.Rooms.FirstActive(adjacentRoom.Key);
				
				if (adjacentRoom.Value)
				{
					if (revealDistance == 0) room.IsRevealed.Value = true;
					else room.RevealDistance.Value = revealDistance;
					continue;
				}

				room.RevealDistance.Value = Mathf.Min(revealDistance + 1, room.RevealDistance.Value);
			}
		}
		
		void OnRoomUpdateConnection(string otherRoomId, bool isOpen)
		{
			if (!Model.AdjacentRoomIds.Value.TryGetValue(otherRoomId, out var oldIsOpen))
			{
				Debug.LogError("AdjacentRoomId was never cached, this should never happen: " + otherRoomId);
				return;
			}

			if (isOpen == oldIsOpen) return;

			Model.AdjacentRoomIds.Value = Model.AdjacentRoomIds.Value
				.ToDictionary()
				.ToReadonlyDictionary(
					kv => kv.Key,
					kv => kv.Key == otherRoomId ? isOpen : kv.Value
				);

			if (Model.IsRevealed.Value || !isOpen) return;

			if (Game.Rooms.FirstActive(otherRoomId).IsRevealed.Value) Model.IsRevealed.Value = true;
		}

		bool OnRoomBoundaryContains(Vector3 position) => View.BoundaryContains(position);

		Vector3? OnRoomBoundaryRandomPoint(Demon generator)
		{
			if (View.NotVisible) return null;
			return View.BoundaryRandomPoint(generator);
		}
		#endregion
	}
}