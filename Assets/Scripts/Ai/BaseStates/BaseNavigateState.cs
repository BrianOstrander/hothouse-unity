using Lunra.Hothouse.Models;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai
{
	public class BaseNavigateState<S, A> : AgentState<GameModel, A>
		where S : AgentState<GameModel, A>
		where A : AgentModel
	{
		public override string Name => "Navigate";

		public override void OnInitialize()
		{
			AddTransitions(
				new ToReturnOnDone(),
				new ToReturnOnInvalid()
			);
		}

		public override void Begin()
		{
			switch (Agent.NavigationPlan.Value.State)
			{
				case NavigationPlan.States.Navigating:
				case NavigationPlan.States.NavigatingForced:
					break;
				default:
					CalculatePath();
					break;
			}
		}

		public override void Idle()
		{
			if (Agent.NavigationPlan.Value.Created < Game.NavigationMesh.LastUpdated.Value && Agent.NavigationPlan.Value.State != NavigationPlan.States.NavigatingForced)
			{
				if (!CalculatePath()) return;
			}
			
			// TODO: This really should be handled in the presenter's update loop...
			Agent.NavigationPlan.Value = Agent.NavigationPlan.Value.Next(Agent.NavigationVelocity.Value * Game.SimulationTimeDelta);
		}

		bool CalculatePath()
		{
			var endPosition = Agent.NavigationPlan.Value.EndPosition;
			if (!Mathf.Approximately(0f, Agent.NavigationPlan.Value.MaximumThreshold))
			{
				var hasSample = NavMesh.SamplePosition(
					endPosition,
					out var sampleHit,
					Agent.NavigationPlan.Value.MaximumThreshold,
					NavMesh.AllAreas
				);
				if (hasSample) endPosition = sampleHit.position;
				else
				{
					Agent.NavigationPlan.Value = NavigationPlan.Invalid(Agent.NavigationPlan.Value);
					return false;
				}
			}
			
			var path = new NavMeshPath();
			var hasPath = NavMesh.CalculatePath(
				Agent.NavigationPlan.Value.Position,
				endPosition,
				NavMesh.AllAreas,
				path
			);

			if (hasPath) Agent.NavigationPlan.Value = NavigationPlan.Navigating(path, Agent.NavigationPlan.Value.Interrupt, Agent.NavigationPlan.Value.RadiusThreshold, Agent.NavigationPlan.Value.PathThreshold);
			else Agent.NavigationPlan.Value = NavigationPlan.Invalid(Agent.NavigationPlan.Value);

			return hasPath;
		}

		class ToReturnOnDone : AgentTransition<S, GameModel, A>
		{
			public override bool IsTriggered()
			{
				switch (Agent.NavigationPlan.Value.State)
				{
					case NavigationPlan.States.Done:
						return true;
				}
				return false;
			}
		}
		
		class ToReturnOnInvalid : AgentTransition<S, GameModel, A>
		{
			public override bool IsTriggered()
			{
				switch (Agent.NavigationPlan.Value.State)
				{
					case NavigationPlan.States.Invalid:
						return true;
				}
				return false;
			}
		}
	}
}