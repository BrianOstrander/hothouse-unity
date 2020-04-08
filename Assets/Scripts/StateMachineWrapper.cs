using System;

namespace LunraGames.SubLight
{
	public class StateMachineWrapper
	{
		Type type;
		StateMachine stateMachine;

		public StateMachineWrapper(StateMachine stateMachine, Type type)
		{
			this.type = type;
			this.stateMachine = stateMachine;
		}

		public string UniqueSynchronizedId { get { return Guid.NewGuid().ToString(); } }

		#region StateMachine Helper Methods
		public void Push(
			Action action,
			string description,
			bool repeating = false,
			string synchronizedId = null
		)
		{
			stateMachine.Push(action, type, description, repeating, synchronizedId);
		}

		public void PushBlocking(
			Action<Action> action,
			string description,
			string synchronizedId = null
		)
		{
			stateMachine.PushBlocking(action, type, description, synchronizedId);
		}

		public void PushBlocking(
			Action action,
			Func<bool> condition,
			string description,
			string synchronizedId = null
		)
		{
			stateMachine.PushBlocking(action, condition, type, description, synchronizedId);
		}

		public void PushBlocking(
			Func<bool> condition,
			string description,
			string synchronizedId = null
		)
		{
			stateMachine.PushBlocking(ActionExtensions.Empty, condition, type, description, synchronizedId);
		}

		public void PushBreak()
		{
			stateMachine.PushBreak();
		}
		#endregion
	}
}