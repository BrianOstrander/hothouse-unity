using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public abstract class JobState<S0, S1> : AgentState<GameModel, DwellerModel>
		where S0 : AgentState<GameModel, DwellerModel>
		where S1 : JobState<S0, S1>
	{
		static readonly Buildings[] EmptyWorkplaces = new Buildings[0];
		
		public override string Name => "Job"+Job;

		protected abstract Jobs Job { get; }

		protected virtual Buildings[] Workplaces => EmptyWorkplaces;

		protected bool IsCurrentJob => Job == Agent.Job.Value;

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
		
		#region Child Classes
		protected class CleanupState : CleanupItemDropsState<S1, CleanupState> { }
		
		protected class DestroyMeleeHandlerState : DestroyMeleeHandlerState<S1> { }
		
		protected class InventoryRequestState : InventoryRequestState<S1> { }
		
		// protected class ObligationState : ObligationState<S1> { }
		#endregion
	}
}