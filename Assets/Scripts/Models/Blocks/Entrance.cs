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
			/// <summary>
			/// Is navigable and lit.
			/// </summary>
			Available = 10,
			/// <summary>
			/// May or may not be navigable, but it's not lit.
			/// </summary>
			NotAvailable = 20
		}
			
		public readonly Vector3 Position;
		public readonly Vector3 Forward;
		public readonly bool IsNavigable;
		/// <summary>
		/// When Available, that means this entrance is navigable and lit.
		/// </summary>
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