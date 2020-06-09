using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class RoomPrefabPresenter : PrefabPresenter<RoomPrefabModel, RoomPrefabView>
	{
		public RoomPrefabPresenter(GameModel game, RoomPrefabModel model) : base(game, model) { }

		protected override void Bind()
		{
			Game.SimulationTime.Changed += OnGameSimulationTime;

			Model.UpdateConnection += OnRoomUpdateConnection;

			base.Bind();
		}

		protected override void UnBind()
		{
			Game.SimulationTime.Changed -= OnGameSimulationTime;
			
			Model.UpdateConnection -= OnRoomUpdateConnection;

			base.UnBind();
		}

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
		}
		#endregion
	}
}