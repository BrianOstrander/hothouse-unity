using System;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.WildVacuum.Models
{
	public struct NavigationPlan
	{
		public enum States
		{
			Unknown = 0,
			Invalid = 10,
			Calculating = 30,
			Navigating = 40,
			NavigatingForced = 50,
			Done = 60
		}
		
		public static NavigationPlan Calculating(
			Vector3 beginPosition,
			Vector3 endPosition
		)
		{
			return new NavigationPlan(
				new [] { beginPosition, endPosition },
				beginPosition, 
				1,
				States.Calculating
			);
		}
		
		public static NavigationPlan Calculating(
			NavigationPlan navigationPlan
		)
		{
			return new NavigationPlan(
				new [] { navigationPlan.Position, navigationPlan.EndPosition },
				navigationPlan.Position,
				1,
				States.Calculating
			);
		}
		
		public static NavigationPlan Navigating(NavMeshPath path)
		{
			return new NavigationPlan(
				path.corners,
				path.corners?[0] ?? Vector3.zero,
				1,
				States.Navigating
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
				States.NavigatingForced
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
		
		public readonly Vector3[] Nodes;
		public readonly Vector3 Position;
		public readonly States State;
		public readonly DateTime Created;
		
		[JsonProperty] readonly int nextNode;

		[JsonIgnore] public Vector3 BeginPosition => Nodes?.FirstOrDefault() ?? Vector3.zero;
		[JsonIgnore] public Vector3 EndPosition => Nodes?.LastOrDefault() ?? Vector3.zero;
		
		NavigationPlan(
			Vector3[] nodes,
			Vector3 position,
			int nextNode,
			States state
		)
		{
			Nodes = nodes;
			Position = position;
			this.nextNode = nextNode;
			State = state;
			
			Created = DateTime.Now;
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
				
			var newPosition = Position;
			var newNextNode = nextNode;
			var newState = State;
				
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
				break;
			}

			if (newNextNode == Nodes.Length) newState = States.Done;

			return new NavigationPlan(
				Nodes,
				newPosition,
				newNextNode,
				newState
			);
		}
	}
}