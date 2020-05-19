using System;
using System.Linq;

namespace Lunra.Hothouse.Models
{
	public struct LightDelta
	{
		public static LightDelta Default() => new LightDelta(States.Unknown, DateTime.MinValue, null, null);
		public static LightDelta Calculated() => new LightDelta(States.Calculated, DateTime.MinValue, null, null);
		
		public enum States
		{
			Unknown = 0,
			Stale = 10,
			Calculated = 20
		}

		public readonly States State;
		public readonly DateTime LastUpdate;
		public readonly string[] RoomIds;
		public readonly string[] SensitiveIds;

		LightDelta(
			States state,
			DateTime lastUpdate,
			string[] roomIds,
			string[] sensitiveIds
		)
		{
			State = state;
			LastUpdate = lastUpdate;
			RoomIds = roomIds ?? new string[0];
			SensitiveIds = sensitiveIds ?? new string[0];
		}

		public LightDelta SetRoomStale(params string[] roomIds)
		{
			return new LightDelta(
				States.Stale,
				DateTime.Now,
				RoomIds.Union(roomIds).ToArray(),
				SensitiveIds
			);
		}
		
		public LightDelta SetSensitiveStale(params string[] sensitiveIds)
		{
			return new LightDelta(
				States.Stale,
				DateTime.Now,
				RoomIds,
				SensitiveIds.Union(sensitiveIds).ToArray()
			);
		}
	}
}