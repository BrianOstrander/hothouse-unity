using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public abstract class JobState<S0, S1> : AgentState<GameModel, DwellerModel>
		where S0 : AgentState<GameModel, DwellerModel>
		where S1 : JobState<S0, S1>
	{
		class WorkplaceNavigationCache
		{
			public DateTime LastUpdated;
			public bool IsCurrentlyAtWorkplace;
			public Navigation.Result NavigationToWorkplace;

			public WorkplaceNavigationCache()
			{
				LastUpdated = DateTime.Now;
			}
		}
		
		static readonly BuildingTypes[] EmptyWorkplaces = new BuildingTypes[0];
		
		public override string Name => "Job"+Job;

		protected abstract Jobs Job { get; }

		protected virtual BuildingTypes[] Workplaces => EmptyWorkplaces;

		protected bool IsCurrentJob => Job == Agent.Job.Value;

		protected BuildingModel Workplace { get; private set; }

		WorkplaceNavigationCache workplaceNavigation;
		
		public override void Begin()
		{
			workplaceNavigation = null;
			if (Workplaces.None()) return;
			
			if (Agent.Workplace.Value.TryGetInstance<BuildingModel>(Game, out var workplace)) Workplace = workplace;
			else Debug.LogError("Job " + Job + "requires workplace but was unable to find it, this is an invalid state");
		}

		/// <summary>
		/// Checks if the agent is already at the workplace or if it can navigate to the workplace.
		/// </summary>
		/// <param name="isCurrentlyAtWorkplace"></param>
		/// <param name="navigationToWorkplace"></param>
		/// <returns>Returns true if the agent is at the workplace or can navigate to it.</returns>
		protected bool TryCalculateWorkplaceNavigation(
			out bool isCurrentlyAtWorkplace,
			out Navigation.Result navigationToWorkplace
		)
		{
			if (workplaceNavigation != null && Game.NavigationMesh.LastUpdated.Value <= workplaceNavigation.LastUpdated)
			{
				isCurrentlyAtWorkplace = workplaceNavigation.IsCurrentlyAtWorkplace;
				navigationToWorkplace = workplaceNavigation.NavigationToWorkplace;
				return isCurrentlyAtWorkplace || navigationToWorkplace.IsValid;
			}
			
			workplaceNavigation = new WorkplaceNavigationCache();
			
			isCurrentlyAtWorkplace = false;
			navigationToWorkplace = default;

			if (Workplaces.None())
			{
				Debug.LogError("Job "+Job+" with no specified workplaces is trying to calculate its workplace, this is invalid");
				return false;
			}

			if (Workplace == null)
			{
				Debug.LogError("Missing workplace, this is invalid");
				return false;
			}

			if (!Navigation.TryQuery(Workplace, out var query))
			{
				Debug.LogError("Unable to query workplace, this should not happen");
				return false;
			}

			if (query.GetMinimumTargetDistance(Agent.Transform.Position.Value) < 0.1f)
			{
				workplaceNavigation.IsCurrentlyAtWorkplace = (isCurrentlyAtWorkplace = true);
				return true;
			}

			var isNavigable = NavigationUtility.CalculateNearest(
				Agent.Transform.Position.Value,
				out workplaceNavigation.NavigationToWorkplace,
				query	
			);
			
			navigationToWorkplace = workplaceNavigation.NavigationToWorkplace;

			return isNavigable;
		}
		
		public class ToJobOnShiftBegin : AgentTransition<S0, S1, GameModel, DwellerModel>
		{
			IClaimOwnershipModel workplaceTarget;
			
			public override bool IsTriggered()
			{
				if (!TargetState.IsCurrentJob) return false;
				if (!Agent.JobShift.Value.Contains(Game.SimulationTime.Value)) return false;

				workplaceTarget = null;
				
				if (TargetState.Workplaces.None()) return true;

				if (Agent.Workplace.Value.TryGetInstance<IClaimOwnershipModel>(Game, out var workplace))
				{
					if (workplace.Ownership.Contains(Agent))
					{
						if (Navigation.TryQuery(workplace, out var currentWorkplaceQuery))
						{
							var isCurrentWorkplaceNavigable = NavigationUtility.CalculateNearest(
								Agent.Transform.Position.Value,
								out _,
								currentWorkplaceQuery
							);

							if (isCurrentWorkplaceNavigable) return true;
							
							workplace.Ownership.Remove(Agent);
							Agent.Workplace.Value = InstanceId.Null();
						}
						else
						{
							Debug.LogError("Invalid query");
						}
					}
				}

				var possibleWorkplaces = Game.Buildings.AllActive
					.Where(m => m.BuildingState.Value == BuildingStates.Operating)
					.Where(m => !m.Ownership.IsFull)
					.Where(m => TargetState.Workplaces.Contains(m.Type.Value))
					.Where(m => m.Enterable.AnyAvailable())
					.OrderBy(m => Vector3.Distance(Agent.Transform.Position.Value, m.Transform.Position.Value))
					.Select(m => Navigation.QueryEntrances(m));

				if (possibleWorkplaces.None()) return false;
				
				var found = NavigationUtility.CalculateNearest(
					Agent.Transform.Position.Value,
					out var workplaceResult,
					possibleWorkplaces.ToArray()
				);

				if (!found) return false;
				
				workplaceTarget = workplaceResult.TargetModel as IClaimOwnershipModel;

				return true;
			}

			public override void Transition()
			{
				if (workplaceTarget == null) return;
				
				Agent.Workplace.Value = InstanceId.New(workplaceTarget);
				workplaceTarget.Ownership.Add(Agent);
			}
		}

		protected class ToReturnOnJobChanged : AgentTransition<S1, S0, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => !SourceState.IsCurrentJob;
		}

		protected class ToReturnOnShiftEnd : AgentTransition<S1, S0, GameModel, DwellerModel>
		{
			public override bool IsTriggered() => !Agent.JobShift.Value.Contains(Game.SimulationTime.Value);
		}
		
		protected class ToReturnOnWorkplaceMissing : AgentTransition<S1, S0, GameModel, DwellerModel>
		{
			public override bool IsTriggered()
			{
				if (Agent.Workplace.Value.IsNull) return true;
				if (SourceState.Workplace == null) return true;
				if (!SourceState.Workplace.Ownership.Contains(Agent)) return true;
				if (!SourceState.Workplace.IsBuildingState(BuildingStates.Operating)) return true;

				return false;
			}

			public override void Transition()
			{
				if (!Agent.Workplace.Value.IsNull) Agent.Workplace.Value = InstanceId.Null();
				SourceState.Workplace?.Ownership.Remove(Agent);
			}
		}

		protected class ToReturnOnWorkplaceIsNotNavigable : AgentTransition<S1, S0, GameModel, DwellerModel>
		{
			string lastWorkplaceId;
			DateTime lastUpdated;
			
			public override bool IsTriggered()
			{
				if (lastWorkplaceId == Agent.Workplace.Value.Id && Game.NavigationMesh.LastUpdated.Value <= lastUpdated) return false;
				if (SourceState.Workplace == null) return false;

				var isNavigableOrAtWorkplace = SourceState.TryCalculateWorkplaceNavigation(
					out _,
					out _
				);

				if (!isNavigableOrAtWorkplace) return true;

				lastWorkplaceId = SourceState.Workplace.Id.Value;
				lastUpdated = DateTime.Now;

				return false;
			}

			public override void Transition()
			{
				if (!Agent.Workplace.Value.IsNull) Agent.Workplace.Value = InstanceId.Null();
				SourceState.Workplace?.Ownership.Remove(Agent);
			}
		}
		
		protected class ToNavigateToWorkplace : AgentTransition<S1, NavigateState, GameModel, DwellerModel>
		{
			Navigation.Result navigationToWorkplace;
			
			public override bool IsTriggered()
			{
				if (SourceState.Workplace == null) return false;

				var isNavigableOrAtWorkplace = SourceState.TryCalculateWorkplaceNavigation(
					out var isCurrentlyAtWorkplace,
					out navigationToWorkplace
				);

				if (isCurrentlyAtWorkplace) return false;

				return isNavigableOrAtWorkplace;
			}

			public override void Transition()
			{
				Agent.NavigationPlan.Value = NavigationPlan.Navigating(navigationToWorkplace.Path);
			}
		}
		
		#region Child Classes
		protected class CleanupState : CleanupItemDropsState<S1, CleanupState> { }
		
		protected class DestroyMeleeHandlerState : DestroyMeleeHandlerState<S1> { }
		protected class ConstructAssembleHandlerState : ConstructAssembleHandlerState<S1> { }
		
		protected class InventoryRequestState : InventoryRequestState<S1> { }
		
		protected class NavigateState : NavigateState<S1> { }
		
		protected class BalanceItemState : BalanceItemState<S1> { } 
		
		// protected class ObligationState : ObligationState<S1> { }
		#endregion
	}
}