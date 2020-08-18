using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.SnapCap
{
	public class IdleState : AgentState<GameModel, SnapCapModel>
	{
		public override void OnInitialize()
		{
			AddChildStates(
				new HuntState()
			);
			
			AddTransitions(
				new HuntState.ToHuntOnAwake()
			);
		}
		
		protected class HuntState : HuntState<IdleState> { }
	}
}