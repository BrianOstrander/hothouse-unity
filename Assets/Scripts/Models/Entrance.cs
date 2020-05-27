using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public struct Entrance
	{
		public enum States
		{
			Unknown = 0,
			Available = 10,
			NotAvailable = 20
		}
			
		public readonly Vector3 Position;
		public readonly bool IsNavigable;
		public readonly States State;

		public Entrance(
			Vector3 position,
			bool isNavigable,
			States state
		)
		{
			Position = position;
			IsNavigable = isNavigable;
			State = state;
		}
	}
}