using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Hothouse.Ai.Dweller
{
	public class DropItemsTransition<S> : AgentTransition<TimeoutState<S>, GameModel, DwellerModel>
		where S : AgentState<GameModel, DwellerModel>
	{
		public override string Name => "DropItems";

		TimeoutState<S> timeoutState;

		public DropItemsTransition(
			TimeoutState<S> timeoutState
		)
		{
			this.timeoutState = timeoutState;
		}

		public override bool IsTriggered() => !Agent.Inventory.All.Value.IsEmpty;

		public override void Transition()
		{
			Game.ItemDrops.Activate(
				"default",
				Agent.RoomTransform.Id.Value,
				Agent.Transform.Position.Value,
				Quaternion.identity,
				itemDrop =>
				{
					itemDrop.Inventory.Add(Agent.Inventory.All.Value);
					itemDrop.Job.Value = Agent.Job.Value;
				}
			);

			Agent.Inventory.Remove(Agent.Inventory.All.Value);
				
			timeoutState.ConfigureForInterval(Interval.WithMaximum(Agent.DepositCooldown.Value));
		}
	}
}