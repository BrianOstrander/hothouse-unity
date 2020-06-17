using System;
using Lunra.Hothouse.Models;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class NavigateToNearestLightTransition<S> : AgentTransition<NavigateState<S>, GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		DateTime lastLightUpdateChecked;
		NavMeshPath path;

		public override string Name => "ToNavigateToNearestLight";

		public override bool IsTriggered()
		{
			if (lastLightUpdateChecked < Game.LastLightUpdate.Value.LastUpdate) return false;
			lastLightUpdateChecked = Game.LastLightUpdate.Value.LastUpdate;
			if (0 < Game.CalculateMaximumLighting((Agent.RoomTransform.Id.Value, Agent.Transform.Position.Value, null)).OperatingMaximum) return false;

			var target = AgentUtility.CalculateNearestAvailableOperatingEntrance(
				Agent.Transform.Position.Value,
				out path,
				out _,
				b => true,
				Game.Buildings.AllActive
			);

			return target != null;
		}

		public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(path);
	}
}