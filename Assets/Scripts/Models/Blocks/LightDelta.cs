using System;
using System.Linq;
using Lunra.Core;
using UnityEngine.Assertions;

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

		public States State { get; }
		public DateTime LastUpdate { get; }
		public string[] RoomIds { get; }
		public string[] SensitiveIds { get; }

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
			Assert.IsFalse(roomIds.None(), "Cannot specify no roomIds");
			Assert.IsFalse(roomIds.Any(r => string.IsNullOrEmpty(r)), "Cannot specify a null or empty roomId");
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