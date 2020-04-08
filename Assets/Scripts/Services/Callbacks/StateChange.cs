namespace LunraGames.SubLight
{
	public struct StateChange
	{
		public readonly StateMachine.States State;
		public readonly StateMachine.Events Event;
		public readonly object Payload;

		public StateChange(StateMachine.States state, StateMachine.Events stateEvent, object payload)
		{
			State = state;
			Event = stateEvent;
			Payload = payload;
		}

		public P GetPayload<P>() where P : class, IStatePayload
		{
			return Payload as P;
		}

		public bool Is(StateMachine.States state, StateMachine.Events stateEvent)
		{
			return State == state && Event == stateEvent;
		}
	}
}