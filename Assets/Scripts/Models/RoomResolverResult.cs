namespace Lunra.Hothouse.Models
{
	public struct RoomResolverResult
	{
		public RoomResolverRequest Request;
		
		public float GenerationElapsed;
		
		public RoomModel[] Rooms;
		public DoorModel[] Doors;

		public override string ToString()
		{
			var result = "Generated in " + GenerationElapsed.ToString("N2") + " seconds";
			result += "\n\tSeed: " + Request.Seed;
			result += "\n\tRooms: " + Rooms.Length;
			result += "\n\tDoors: " + Doors.Length;

			return result;
		}
	}
}