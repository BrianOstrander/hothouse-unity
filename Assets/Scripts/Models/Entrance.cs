using UnityEngine;

namespace Lunra.WildVacuum.Models
{
	public struct Entrance
	{
		public enum States
		{
			Unknown = 0,
			Available = 10,
			Blocked = 20
		}
			
		public readonly Vector3 Position;
		public readonly States State;

		public Entrance(
			Vector3 position,
			States state
		)
		{
			Position = position;
			State = state;
		}
	}
}