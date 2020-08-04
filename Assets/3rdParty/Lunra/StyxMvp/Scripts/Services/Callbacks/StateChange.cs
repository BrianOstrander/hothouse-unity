using System;
using UnityEngine;

namespace Lunra.StyxMvp.Services
{
	public struct StateChange
	{
		public readonly Type State;
		public readonly StateMachine.Events Event;
		public readonly object Payload;

		public StateChange(Type state, StateMachine.Events stateEvent, object payload)
		{
			State = state;
			Event = stateEvent;
			Payload = payload;
		}

		public P GetPayload<P>() where P : class, IStatePayload
		{
			return Payload as P;
		}

		public bool Is<S>(StateMachine.Events stateEvent)
		{
			return State == typeof(S) && Event == stateEvent;
		}
	}
}