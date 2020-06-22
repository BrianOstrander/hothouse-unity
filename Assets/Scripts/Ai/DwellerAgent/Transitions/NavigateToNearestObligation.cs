/*
using System.Linq;
using Lunra.Hothouse.Models;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class NavigateToNearestObligationTransition<S> : AgentTransition<NavigateState<S>, GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		(IObligationModel Model, Obligation Obligation) target;
			
		public override bool IsTriggered()
		{
			if (Agent.Obligation.Value.IsEnabled) return true;
			target = Game.GetObligationsAvailable()
				.GetIndividualObligations(o => o.State == Obligation.States.Available && o.IsValidJob(Agent.Job.Value))
				.OrderBy(e => e.Obligation.Priority)
				.FirstOrDefault();

			if (target.Model == null) return false;

			var result = NavigationUtility.CalculateNearestAvailableEntrance(
				Agent.Transform.Position.Value,
				out _,
				out _,
				target.Model
			);

			return result != null;
		}
		
		public override void Transition()
		{
			var newObligation = target.Obligation.New(Obligation.States.Promised);

			target.Model.Obligations.All.Value = target.Model.Obligations.All.Value
				.Select(o => o.PromiseId == newObligation.PromiseId ? newObligation : o)
				.ToArray();
				
			Agent.Obligation.Value = ObligationPromise.New(
				target.Model.Id.Value,
				newObligation.PromiseId
			);
		}
		
		bool IsNavigableObligation(
			ObligationPromise
		)
		{
			
		}
	}
}
*/