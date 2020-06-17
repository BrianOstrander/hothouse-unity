using System;

namespace Lunra.Hothouse.Models
{
	public struct RoomResolverResult
	{
		public RoomResolverRequest Request;
		
		public TimeSpan GenerationElapsed;
		
		public RoomModel[] Rooms;
		public DoorModel[] Doors;

		public override string ToString()
		{
			var result = "Generated in " + GenerationElapsed.TotalSeconds.ToString("N2") + " seconds";
			// result += "\n\tSeed: " + Request.Generator.see.Seed;
			result += "\n\tRooms: " + Rooms.Length;
			result += "\n\tDoors: " + Doors.Length;

			return result;
		}
	}
}