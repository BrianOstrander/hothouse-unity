using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public static class Interaction
	{
		public enum States
		{
			Unknown = 0,
			OutOfRange = 10,
			Idle = 20,
			Begin = 30,
			Active = 40,
			End = 50,
			Cancel = 60
		}
	
		public struct DeltaFloat
		{
			public static DeltaFloat Default() => new DeltaFloat(0f, 0f);
			public static DeltaFloat New(float value) => new DeltaFloat(value, value);
			
			public float Begin { get; }
			public float End { get; }
			public float Delta { get; }
			
			public float Current => End;

			public DeltaFloat(
				float begin,
				float end
			)
			{
				Begin = begin;
				End = end;
				Delta = end - begin;
			}

			public DeltaFloat NewEnd(float end)
			{
				return new DeltaFloat(
					Begin,
					end
				);
			}
			
			public override string ToString()
			{
				return "[ " + Begin + " , " + End + " ] ";
			}

			public Quaternion GetRotation(Vector3 axis) => Quaternion.AngleAxis(End, axis);
		}
		
		public struct DeltaVector3
		{
			public static DeltaVector3 Default() => new DeltaVector3(Vector3.zero, Vector3.zero);
			public static DeltaVector3 New(Vector3 position) => new DeltaVector3(position, position);
			
			public Vector3 Begin { get; }
			public Vector3 End { get; }
			public Vector3 Delta { get; }
			
			public Vector3 Current => End;

			public DeltaVector3(
				Vector3 begin,
				Vector3 end
			)
			{
				Begin = begin;
				End = end;
				Delta = end - begin;
			}

			public DeltaVector3 NewEnd(Vector3 end)
			{
				return new DeltaVector3(
					Begin,
					end
				);
			}
			
			public bool RadiusContains(Vector3 position) => Vector3.Distance(Begin, position) < Delta.magnitude;

			public override string ToString()
			{
				return "[ " + Begin + " , " + End + " ] ";
			}
		}
		
		public struct Display
		{
			public static Display Default() => new Display(
				States.Idle,
				DeltaVector3.Default(),
				DeltaVector3.Default()
			);
			
			public States State { get; }
			public DeltaVector3 ScreenPosition { get; }
			public DeltaVector3 ViewportPosition { get; }

			public Display(
				States state,
				DeltaVector3 screenPosition,
				DeltaVector3 viewportPosition
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
		
		public struct GenericFloat
		{
			public static GenericFloat Default() => new GenericFloat(States.Idle, new DeltaFloat());
			public static GenericFloat Point(States state, float value) => new GenericFloat(state, DeltaFloat.New(value));
			
			public States State { get; }
			public DeltaFloat Value { get; }

			public GenericFloat(
				States state,
				DeltaFloat value
			)
			{
				State = state;
				Value = value;
			}

			public GenericFloat NewState(States state)
			{
				return new GenericFloat(
					state,
					Value
				);
			}

			public GenericFloat NewEnd(
				States state,
				float valueEnd
			)
			{
				return new GenericFloat(
					state,
					Value.NewEnd(valueEnd)
				);
			}

			public override string ToString()
			{
				return nameof(Interaction) + "." + nameof(GenericFloat) + ": "+State+"\n"+Value;
			}
		}
		
		public struct GenericVector3
		{
			public static GenericVector3 Default() => new GenericVector3(States.Idle, new DeltaVector3());
			public static GenericVector3 Point(States state, Vector3 value) => new GenericVector3(state, DeltaVector3.New(value));
			
			public States State { get; }
			public DeltaVector3 Value { get; }

			public GenericVector3(
				States state,
				DeltaVector3 value
			)
			{
				State = state;
				Value = value;
			}

			public GenericVector3 NewState(States state)
			{
				return new GenericVector3(
					state,
					Value
				);
			}

			public GenericVector3 NewEnd(
				States state,
				Vector3 valueEnd
			)
			{
				return new GenericVector3(
					state,
					Value.NewEnd(valueEnd)
				);
			}

			public override string ToString()
			{
				return nameof(Interaction) + "." + nameof(GenericVector3) + ": "+State+"\n"+Value;
			}
		}
		
		public struct RoomVector3
		{
			public static RoomVector3 Default() => new RoomVector3(States.Idle, null, new DeltaVector3());
			public static RoomVector3 Point(States state, string roomId, Vector3 value) => new RoomVector3(state, roomId, DeltaVector3.New(value));
			
			public States State { get; }
			public string RoomId { get; }
			public DeltaVector3 Value { get; }

			public RoomVector3(
				States state,
				string roomId,
				DeltaVector3 value
			)
			{
				State = state;
				RoomId = roomId;
				Value = value;
			}

			public RoomVector3 NewState(States state)
			{
				return new RoomVector3(
					state,
					RoomId,
					Value
				);
			}

			public RoomVector3 NewEnd(
				States state,
				Vector3 valueEnd
			)
			{
				return new RoomVector3(
					state,
					RoomId,
					Value.NewEnd(valueEnd)
				);
			}

			public override string ToString()
			{
				return nameof(Interaction) + "." + nameof(RoomId) + ": " + State + "\n" + RoomId + " , " + Value;
			}
		}
	}
}