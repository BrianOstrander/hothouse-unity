using System;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Ai
{
	public class DwellerNavigateToNearestLightTransition<S> : AgentTransition<DwellerNavigateState<S>, GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		DateTime lastLightUpdateChecked;
		NavMeshPath path;

		public override string Name => "ToNavigateToNearestLight";

		public override bool IsTriggered()
		{
			if (lastLightUpdateChecked < World.LastLightUpdate.Value.LastUpdate) return false;
			lastLightUpdateChecked = World.LastLightUpdate.Value.LastUpdate;
			if (0 < World.CalculateMaximumLighting((Agent.RoomId.Value, Agent.Transform.Position.Value)).OperatingMaximum) return false;

			var target = DwellerUtility.CalculateNearestAvailableOperatingEntrance(
				Agent.Transform.Position.Value,
				out path,
				out _,
				b => true,
				World.Buildings.AllActive
			);

			return target != null;
		}

		public override void Transition() => Agent.NavigationPlan.Value = NavigationPlan.Navigating(path);
	}
}