using System;

using UnityEngine;

namespace Lunra.StyxMvp.Services
{
	public interface IStatePayload {}

	public interface IState
	{
		Type PayloadType { get; }
		bool AcceptsPayload(object payload, bool throws = false);
		void Initialize(object payload);
		void UpdateState(Type state, StateMachine.Events stateEvent, object payload);
	}

	public interface IStateTyped<P> : IState
		where P : class
	{
		P Payload { get; }
	}

	public abstract class BaseState : IState
	{
		public abstract Type PayloadType { get; }

		public object PayloadObject { get; set; }
		
		public virtual bool AcceptsPayload(object payload, bool throws = false)
		{
			Exception exception = null;
			if (payload == null) exception = new ArgumentNullException(nameof(payload));
			else if (payload.GetType() != PayloadType) exception = new ArgumentException("payload of type " + payload.GetType() + " is not supported by this state");

			var accepts = exception == null;
			if (throws && !accepts) throw exception;
			return accepts;
		}

		public virtual void Initialize(object payload)
		{
			AcceptsPayload(payload, true);
			PayloadObject = payload;
		}

		public virtual void UpdateState(Type state, StateMachine.Events stateEvent, object payload)
		{
			if (state != GetType()) return;
			switch (stateEvent)
			{
				case StateMachine.Events.Begin:
					Begin();
					break;
				case StateMachine.Events.Idle:
					Idle();
					break;
				case StateMachine.Events.End:
					End();
					break;
			}

			if (StateMachine.LogStateChanges.Value) Debug.Log("<b>" + state.Name + "." + stateEvent + "</b> ( " + payload.GetType().Name + " )");
			App.S.StateChange(new StateChange(state, stateEvent, payload));
		}

		protected virtual void Begin() { }
		protected virtual void End() { }
		protected virtual void Idle() { }
	}

	public abstract class State<P> : BaseState, IStateTyped<P>
		where P : class, IStatePayload
	{
		public override Type PayloadType => typeof(P);

		public P Payload
		{
			get => PayloadObject as P;
			set => PayloadObject = value;
		}
	}
}