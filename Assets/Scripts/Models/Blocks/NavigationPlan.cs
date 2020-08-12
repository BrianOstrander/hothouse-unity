using System;
using System.Linq;
using Lunra.Core;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Models
{
	public struct NavigationPlan
	{
		[Flags]
		public enum Interrupts
		{
			None = 0,
			LineOfSight = 1 << 0,
			RadiusThreshold = 1 << 1,
			PathThreshold = 1 << 2
		}
		
		public enum States
		{
			Unknown = 0,
			Invalid = 10,
			Calculating = 30,
			Navigating = 40,
			NavigatingForced = 50,
			Done = 60
		}
		
		// public static NavigationPlan Calculating(
		// 	Vector3 beginPosition,
		// 	Vector3 endPosition,
		// 	float threshold = 0f
		// )
		// {
		// 	return new NavigationPlan(
		// 		new [] { beginPosition, endPosition },
		// 		beginPosition, 
		// 		1,
		// 		States.Calculating,
		// 		threshold
		// 	);
		// }
		//
		// public static NavigationPlan Calculating(
		// 	NavigationPlan navigationPlan
		// )
		// {
		// 	return new NavigationPlan(
		// 		new [] { navigationPlan.Position, navigationPlan.EndPosition },
		// 		navigationPlan.Position,
		// 		1,
		// 		States.Calculating,
		// 		navigationPlan.Threshold
		// 	);
		// }
		//
		public static NavigationPlan Navigating(
			NavMeshPath path,
			Interrupts interrupt = Interrupts.None,
			float radiusThreshold = 0f,
			float pathThreshold = 0f
		)
		{
			return new NavigationPlan(
				path.corners,
				path.corners?[0] ?? Vector3.zero,
				1,
				States.Navigating,
				path.corners.TotalDistance(),
				0f,
				interrupt,
				radiusThreshold,
				pathThreshold
			);
		}
		
		public static NavigationPlan NavigatingForced(
			Vector3 beginPosition,
			Vector3 endPosition
		)
		{
			return new NavigationPlan(
				new [] { beginPosition, endPosition },
				beginPosition,
				1,
				States.NavigatingForced,
				Vector3.Distance(beginPosition, endPosition)
			);
		}

		public static NavigationPlan Invalid(NavigationPlan navigationPlan) => Invalid(navigationPlan.Position, navigationPlan.EndPosition);

		public static NavigationPlan Invalid(Vector3 beginPosition) => Invalid(beginPosition, beginPosition);
		
		public static NavigationPlan Invalid(
			Vector3 beginPosition,
			Vector3 endPosition
		)
		{
			return new NavigationPlan(
				new [] { beginPosition, endPosition },
				beginPosition, 
				1,
				States.Invalid
			);
		}

		public static NavigationPlan Done() => Done(Vector3.zero); 
		
		public static NavigationPlan Done(Vector3 position)
		{
			return new NavigationPlan(
				null,
				position,
				-1,
				States.Done
			);
		}
		
		[JsonProperty] public Vector3[] Nodes { get; private set; }
		[JsonProperty] public Vector3 Position { get; private set; }
		[JsonProperty] public States State { get; private set; }
		[JsonProperty] public float DistanceTotal { get; private set; }
		[JsonProperty] public float DistanceElapsed { get; private set; }
		
		
		[JsonProperty] public DateTime Created { get; private set; }
		
		[JsonProperty] public Interrupts Interrupt { get; private set; }
		[JsonProperty] public float RadiusThreshold { get; private set; }
		[JsonProperty] public float PathThreshold { get; private set; }
		
		[JsonProperty] readonly int nextNode;

		[JsonIgnore] public Vector3 BeginPosition => Nodes?.FirstOrDefault() ?? Vector3.zero;
		[JsonIgnore] public Vector3 EndPosition => Nodes?.LastOrDefault() ?? Vector3.zero;
		[JsonIgnore] public float MaximumThreshold => Mathf.Max(PathThreshold, RadiusThreshold);
		
		NavigationPlan(
			Vector3[] nodes,
			Vector3 position,
			int nextNode,
			States state,
			float distanceTotal = 0f,
			float distanceElapsed = 0f,
			Interrupts interrupt = Interrupts.None,
			float radiusThreshold = 0f,
			float pathThreshold = 0f
		)
		{
			Nodes = nodes;
			Position = position;
			this.nextNode = nextNode;
			State = state;
			DistanceTotal = distanceTotal;
			DistanceElapsed = distanceElapsed;
			
			Created = DateTime.Now;

			Interrupt = interrupt;
			RadiusThreshold = radiusThreshold;
			PathThreshold = pathThreshold;
		}

		public NavigationPlan Next(float velocity)
		{
			switch (State)
			{
				case States.Done:
				case States.Invalid:
				case States.Calculating:
					return this;
			}

			var velocityOriginal = velocity;
				
			var newPosition = Position;
			var newNextNode = nextNode;
			var newState = State;
			var newDistanceElapsed = DistanceElapsed;
			
			while (newNextNode < Nodes.Length && 0f < velocity)
			{		
				var distanceToNextNode = Vector3.Distance(newPosition, Nodes[newNextNode]);
					
				if (distanceToNextNode <= velocity)
				{
					newPosition = Nodes[newNextNode];
					newNextNode++;
					velocity -= distanceToNextNode;
					continue;
				}

				newPosition += (Nodes[newNextNode] - newPosition).normalized * velocity;
				velocity = 0f;
				break;
			}

			newDistanceElapsed = DistanceElapsed + (velocityOriginal - velocity);

			if (newNextNode == Nodes.Length) newState = States.Done;
			else if (Interrupt != Interrupts.None)
			{
				var allInterruptsMatched = true;
				foreach (var interrupt in EnumExtensions.GetValues(Interrupts.None))
				{
					if (Interrupt.HasFlag(interrupt))
					{
						switch (interrupt)
						{
							case Interrupts.PathThreshold:
								allInterruptsMatched = newDistanceElapsed <= PathThreshold;
								break;
							case Interrupts.RadiusThreshold:
								allInterruptsMatched = Vector3.Distance(newPosition, EndPosition) <= RadiusThreshold;
								break;
							case Interrupts.LineOfSight:
								
								// TODO: Probably shouldn't hardcode these vertical offsets...
								var begin = newPosition + (Vector3.up * 0.5f); 
								var end = EndPosition + (Vector3.up * 0.5f);
								
								allInterruptsMatched = Physics.Raycast(
									begin,
									(end - begin).normalized,
									Vector3.Distance(begin, end),
									LayerMasks.DefaultAndFloor
								);
								break;
							default:
								Debug.LogError("Unrecognized Interrupt: "+interrupt);
								break;
						}

						if (!allInterruptsMatched) break;
					}
				}

				if (allInterruptsMatched) newState = States.Done;
			}
			
			return new NavigationPlan(
				Nodes,
				newPosition,
				newNextNode,
				newState,
				DistanceTotal,
				newDistanceElapsed,
				Interrupt,
				RadiusThreshold,
				PathThreshold
			);
		}
	}
}