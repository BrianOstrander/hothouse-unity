using System.Collections.Generic;
using Lunra.Hothouse.Views;

namespace Lunra.Hothouse.Models
{
	public struct RoomResolverRequest
	{
		public int Seed;
		public int RoomCountMinimum;
		public int RoomCountMaximum;

		public RoomResolverRequest(
			int seed,
			int roomCountMinimum,
			int roomCountMaximum
		)
		{
			Seed = seed;
			RoomCountMinimum = roomCountMinimum;
			RoomCountMaximum = roomCountMaximum;
		}
	}
}