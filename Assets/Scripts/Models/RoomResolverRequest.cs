using System.Collections.Generic;
using Lunra.Hothouse.Views;

namespace Lunra.Hothouse.Models
{
	public struct RoomResolverRequest
	{
		public int Seed;
		public int RoomCountMinimum;
		public int RoomCountMaximum;
		public float Timeout;

		public RoomResolverRequest(
			int seed,
			int roomCountMinimum,
			int roomCountMaximum,
			float timeout
		)
		{
			Seed = seed;
			RoomCountMinimum = roomCountMinimum;
			RoomCountMaximum = roomCountMaximum;
			Timeout = timeout;
		}
	}
}