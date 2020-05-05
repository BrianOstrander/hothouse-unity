using System.Linq;
using Lunra.Core;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.WildVacuum.Ai
{
	public class DwellerIdleState : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "Idle";

		public override void OnInitialize()
		{
			InstantiateJob<DwellerClearFloraJobState>();
		}

		void InstantiateJob<S>()
			where S : DwellerJobState<S>, new()
		{
			var job = new S();
			AddChildStates(job);
			AddTransitions(job.GetToJobTransition);
		}

		/*
		public override void OnInitialize()
		{
			AddChildStates(
				new DwellerNavigateState<DwellerIdleState>()	
			);
			
			AddTransitions(
				new DwellerNavigationForcedTransition<DwellerIdleState>(),
				new DwellerNavigationTransition<DwellerIdleState>(),
				new ToNavigateToNearestItemCache(),
				new ToClearNearestFlora(),
				new ToNavigateToNearestFlora()
			);
		}

		class ToNavigateToNearestItemCache : AgentTransition<DwellerNavigateState<DwellerIdleState>, GameModel, DwellerModel>
		{
			ItemCacheBuildingModel targetItemCache;
			
			public override bool IsTriggered()
			{
				if (Agent.Inventory.Value.IsEmpty || Agent.Inventory.Value.IsNoneFull()) return false;

				var fullItems = Agent.Inventory.Value.GetFull();

				targetItemCache = World.ItemCaches.Value
					.OrderBy(b => Vector3.Distance(Agent.Position.Value, b.Position.Value))
					.FirstOrDefault(
						b =>
						{
							if (b.Entrances.Value.None(e => e.State == BuildingModel.Entrance.States.Available)) return false;
							
							foreach (var item in fullItems)
							{
								if (!b.Inventory.Value.IsFull(item.Type)) return true;
							}

							return false;
						}
					);

				return targetItemCache != null;
			}

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Calculating(
					Agent.Position.Value,
					targetItemCache.Entrances.Value.FirstOrDefault(e => e.State == BuildingModel.Entrance.States.Available).Position
				);
			}
		}
		
		class ToNavigateToNearestFlora : AgentTransition<DwellerNavigateState<DwellerIdleState>, GameModel, DwellerModel>
		{
			FloraModel targetFlora;
			
			public override bool IsTriggered()
			{
				if (Agent.Job.Value != DwellerModel.Jobs.ClearFlora) return false;

				targetFlora = World.Flora.GetActive()
					.Where(f => f.MarkedForClearing.Value)
					.OrderBy(f => Vector3.Distance(Agent.Position.Value, f.Position.Value))
					.ElementAtOrDefault(Agent.JobPriority.Value);

				return targetFlora != null;
			}

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Calculating(
					Agent.Position.Value,
					targetFlora.Position.Value,
					Agent.MeleeRange.Value
				);
			}
		}
		
		class ToClearNearestFlora : AgentTransition<DwellerClearFloraState, GameModel, DwellerModel>
		{
			public override bool IsTriggered()
			{
				if (Agent.Job.Value != DwellerModel.Jobs.ClearFlora) return false;

				var validFlora = World.Flora.GetActive().FirstOrDefault(
					flora =>
					{
						if (flora.State.Value == FloraModel.States.Pooled) return false;
						if (!flora.MarkedForClearing.Value) return false;
						return Vector3.Distance(Agent.Position.Value, flora.Position.Value) < Agent.MeleeRange.Value;
					}
				);

				return validFlora != null;
			}
		}
		*/
	}
}