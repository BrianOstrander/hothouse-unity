using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public static class Interaction
	{
		public enum States
		{
			Unknown = 0,
			Idle = 10,
			Begin = 20,
			Active = 30,
			End = 40
		}

		public struct Vector3Delta
		{
			public static Vector3Delta Default() => new Vector3Delta(Vector3.zero, Vector3.zero);
			public static Vector3Delta Point(Vector3 position) => new Vector3Delta(position, position);
			
			public Vector3 Begin { get; }
			public Vector3 End { get; }
			public Vector3 Delta { get; }
			
			public Vector3 Current => End;

			public Vector3Delta(
				Vector3 begin,
				Vector3 end
			)
			{
				Begin = begin;
				End = end;
				Delta = end - begin;
			}

			public Vector3Delta NewEnd(Vector3 end)
			{
				return new Vector3Delta(
					Begin,
					end
				);
			}

			public override string ToString()
			{
				return "[ " + Begin + " , " + End + " ] ";
			}
		}
		
		public struct Display
		{
			public static Display Default() => new Display(
				States.Idle,
				Vector3Delta.Default(),
				Vector3Delta.Default()
			);
			
			public States State { get; }
			public Vector3Delta ScreenPosition { get; }
			public Vector3Delta ViewportPosition { get; }

			public Display(
				States state,
				Vector3Delta screenPosition,
				Vector3Delta viewportPosition
			)
			{
				State = state;
				ScreenPosition = screenPosition;
				ViewportPosition = viewportPosition;
			}

			public Display NewEnds(
				States state,
				Vector3 screenPositionEnd,
				Vector3 viewportPositionEnd
			)
			{
				return new Display(
					state,
					ScreenPosition.NewEnd(screenPositionEnd),
					ViewportPosition.NewEnd(viewportPositionEnd)
				);
			}
		}
		
		public struct Generic
		{
			public static Generic Default() => new Generic(States.Idle, new Vector3Delta());
			public static Generic Idle(Vector3 position) => new Generic(States.Idle, Vector3Delta.Point(position));
			public static Generic Begin(Vector3 position) => new Generic(States.Begin, Vector3Delta.Point(position));
			
			public States State { get; }
			public Vector3Delta Position { get; }

			public Generic(
				States state,
				Vector3Delta position
			)
			{
				State = state;
				Position = position;
			}

			public Generic NewEnd(
				States state,
				Vector3 positionEnd
			)
			{
				return new Generic(
					state,
					Position.NewEnd(positionEnd)
				);
			}

			public override string ToString()
			{
				return nameof(Interaction) + "." + nameof(Generic) + ": "+State+"\n"+Position;
			}
		}
	}
}