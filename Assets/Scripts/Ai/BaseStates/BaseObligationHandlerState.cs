using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public abstract class BaseObligationHandlerState<S0, S1, A> : AgentState<GameModel, A>
		where S0 : AgentState<GameModel, A>
		where S1 : BaseObligationHandlerState<S0, S1, A>
		where A : AgentModel
	{
		public abstract Obligation[] ObligationsHandled { get; }

		public class ToObligationHandlerOnAvailableObligation : AgentTransition<S0, S1, GameModel, A>
		{
			public override bool IsTriggered()
			{
				if (!Game.Cache.Value.AnyObligationsAvailable) return false;
				if (TargetState.ObligationsHandled.None(o => Game.Cache.Value.UniqueObligationsAvailable.Contains(o.Type))) return false;

				Debug.Log("sum avail");
				
				return true;
			}
		}
	}
}