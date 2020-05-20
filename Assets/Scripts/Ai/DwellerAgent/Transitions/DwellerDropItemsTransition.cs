using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
using UnityEngine;

namespace Lunra.Hothouse.Ai
{
	public class DwellerDropItemsTransition<S> : AgentTransition<DwellerTimeoutState<S>, GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		DwellerTimeoutState<S> timeoutState;

		public DwellerDropItemsTransition(
			DwellerTimeoutState<S> timeoutState
		)
		{
			this.timeoutState = timeoutState;
		}

		public override bool IsTriggered() => !Agent.Inventory.Value.IsEmpty;

		public override void Transition()
		{
			World.ItemDrops.Activate(
				"default",
				Agent.RoomId.Value,
				Agent.Position.Value,
				Quaternion.identity,
				itemDrop =>
				{
					itemDrop.Inventory.Value = Agent.Inventory.Value;
					itemDrop.Job.Value = Agent.Job.Value;
				}
			);
				
			Agent.Inventory.Value = Inventory.Empty;
				
			timeoutState.ConfigureForInterval(Interval.WithMaximum(Agent.DepositCooldown.Value));
		}
	}
}