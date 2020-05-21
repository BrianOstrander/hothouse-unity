using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public struct Interaction
	{
		public static Interaction None() => new Interaction(Types.None);

		public static Interaction AddClearanceIdle(Vector3 beginPosition)
		{
			return new Interaction(
				Types.AddClearance,
				States.Idle,
				beginPosition,
				beginPosition
			);
		}
		
		public static Interaction ConstructIdle(
			Vector3 beginPosition,
			Buildings building
		)
		{
			return new Interaction(
				Types.AddClearance,
				States.Idle,
				beginPosition,
				beginPosition,
				building
			);
		}
		
		
		public enum Types
		{
			Unknown = 0,
			None = 10,
			AddClearance = 20,
			Construct = 30
		}

		public enum States
		{
			Unknown = 0,
			Idle = 10,
			Begin = 20,
			Select = 30,
			Released = 40
		}

		public readonly Types Type;
		public readonly States State;
		public readonly Vector3 BeginPosition;
		public readonly Vector3 EndPosition;

		#region Types.Construct
		public readonly Buildings Building;
		#endregion
		
		Interaction(
			Types type = Types.None,
			States state = States.Idle,
			Vector3 beginPosition = default,
			Vector3 endPosition = default,
			Buildings building = default
		)
		{
			Type = type;
			State = state;
			BeginPosition = beginPosition;
			EndPosition = endPosition;

			Building = building;
		}

		public Interaction NewState(States state)
		{
			return New(
				state: state
			);
		}

		public Interaction New(
			Types? type = null,
			States? state = null,
			Vector3? beginPosition = null,
			Vector3? endPosition = null
		)
		{
			return new Interaction(
				type ?? Type,
				state ?? State,
				beginPosition ?? BeginPosition,
				endPosition ?? EndPosition,
				Building
			);
		}
	}
}