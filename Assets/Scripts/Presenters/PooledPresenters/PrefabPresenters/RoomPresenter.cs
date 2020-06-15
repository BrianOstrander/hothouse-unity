using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class RoomPresenter : PrefabPresenter<RoomModel, RoomView>
	{
		public RoomPresenter(GameModel game, RoomModel model) : base(game, model) { }

		protected override void Bind()
		{
			Game.SimulationTime.Changed += OnGameSimulationTime;

			Model.IsRevealed.Changed += OnRoomIsRevealed;
			
			Model.UpdateConnection += OnRoomUpdateConnection;

			base.Bind();
		}

		protected override void UnBind()
		{
			Game.SimulationTime.Changed -= OnGameSimulationTime;
			
			Model.IsRevealed.Changed -= OnRoomIsRevealed;

			Model.UpdateConnection -= OnRoomUpdateConnection;

			base.UnBind();
		}
		
		#region View Events
		protected override void OnViewPrepare()
		{
			OnRoomIsRevealed(Model.IsRevealed.Value);
			View.RoomId = Model.Id.Value;
		}
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
		void OnRoomIsRevealed(bool isExplored)
		{
			if (View.NotVisible) return;
			
			View.IsRevealed = Model.IsRevealed.Value;
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
		#endregion
	}
}