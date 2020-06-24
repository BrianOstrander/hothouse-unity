using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Models
{
	public struct Entrance
	{
		// TODO: This shouldn't be hardcoded...
		public const float RangeMaximum = 0.1f;
		public const int DefaultMask = NavMesh.AllAreas;
		
		public enum States
		{
			Unknown = 0,
			Available = 10,
			NotAvailable = 20
		}
			
		public readonly Vector3 Position;
		public readonly Vector3 Forward;
		public readonly bool IsNavigable;
		public readonly States State;

		public Entrance(
			Vector3 position,
			Vector3 forward,
			bool isNavigable,
			States state
		)
		{
			Position = position;
			Forward = forward;
			IsNavigable = isNavigable;
			State = state;
		}
	}
}