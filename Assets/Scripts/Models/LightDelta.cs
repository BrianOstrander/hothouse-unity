using System;
using System.Linq;

namespace Lunra.Hothouse.Models
{
	public struct LightDelta
	{
		public static LightDelta Default() => new LightDelta(States.Unknown, DateTime.MinValue);
		public static LightDelta Calculated() => new LightDelta(States.Calculated, DateTime.MinValue);
		
		public enum States
		{
			Unknown = 0,
			Stale = 10,
			Calculated = 20
		}

		public readonly States State;
		public readonly DateTime LastUpdate;
		public readonly string[] RoomIds;

		LightDelta(
			States state,
			DateTime lastUpdate,
			params string[] roomIds
		)
		{
			State = state;
			LastUpdate = lastUpdate;
			RoomIds = roomIds;
		}

		public LightDelta GetStale(params string[] roomIds)
		{
			return new LightDelta(
				States.Stale,
				DateTime.Now,
				RoomIds.Union(roomIds).ToArray()
			);
		}
	}
}