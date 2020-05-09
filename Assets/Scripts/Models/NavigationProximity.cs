using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public struct NavigationProximity
	{
		public enum AccessStates
		{
			Unknown = 0,
			Accessible = 10,
			NotAccessible = 20
		}

		public readonly Vector3 Position;
		public readonly float Distance;
		public readonly AccessStates Access;

		public NavigationProximity(
			Vector3 position,
			float distance,
			AccessStates access
		)
		{
			Position = position;
			Distance = distance;
			Access = access;
		}
	}
}