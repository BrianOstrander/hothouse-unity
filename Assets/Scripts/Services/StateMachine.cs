using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

namespace LunraGames.SubLight
{
	public class StateMachine
	{
		public enum States
		{
			Unknown = 0,
			Initialize = 10,
			Transition = 20,
			Home = 30,
			Game = 40,
		}

		public enum Events
		{
			Unknown = 0,
			Begin = 10,
			Idle = 20,
			End = 30
		}

		public enum EntryStates
		{
			Unknown = 0,
			Queued = 10,
			Waiting = 20,
			Calling = 30,
			Blocking = 40,
			Blocked = 50
		}

		/// <summary>
		/// Used for external examination and debugging of the StateMachine.
		/// </summary>
		public interface IEntryImmutable
		{
			States State { get; }
			Events Event { get; }
			string Description { get; }
			bool IsRepeating { get; }
			string SynchronizedId { get; }
			EntryStates EntryState { get; }
		}

		interface IEntry : IEntryImmutable
		{
			bool Trigger();

			new EntryStates EntryState { get; set; }
		}

		abstract class Entry : IEntry
		{
			public States State { get; protected set; }
			public Events Event { get; protected set; }
			public string Description { get; protected set; }
			public bool IsRepeating { get; protected set; }
			public string SynchronizedId { get; protected set; }
			public abstract bool Trigger();

			#region Debug Values
			// These are only assigned by the StateMachine, not read for any logical purposes.
			public EntryStates EntryState { get; set; }
   			#endregion
		}

		class BlockingEntry : Entry
		{
			Action<Action> action;
			bool? isDone;

			public BlockingEntry(
				Action<Action> action,
				States state,
				Events stateEvent,
				string description,
				string synchronizedId
			)
			{
				this.action = action;
				State = state;
				Event = stateEvent;
				Description = description;
				IsRepeating = false;
				SynchronizedId = string.IsNullOrEmpty(synchronizedId) ? null : synchronizedId;
			}

			public override bool Trigger()
			{
				if (!isDone.HasValue)
				{
					isDone = false;
					action(OnDone);
				}
				return isDone.Value;
			}

			void OnDone()
			{
				isDone = true;
			}
		}

		class NonBlockingEntry : Entry
		{
			Action action;

			public NonBlockingEntry(
				Action action,
				States state,
				Events stateEvent,
				string description,
				bool repeating,
				string synchronizedId
			)
			{
				this.action = action;
				State = state;
				Event = stateEvent;
				Description = description;
				IsRepeating = repeating;
				SynchronizedId = string.IsNullOrEmpty(synchronizedId) ? null : synchronizedId;
			}

			public override bool Trigger()
			{
				action();
				return true;
			}
		}

		Heartbeat heartbeat;

		IState[] stateEntries;

		IState currentState;
		object currentPayload;
		IState nextState;
		object nextPayload;
		Events currentEvent;
		List<IEntry> queued = new List<IEntry>();
		List<IEntry> entries = new List<IEntry>();

		public States CurrentState 
		{ 
			get 
			{
				if (currentState == null) return States.Unknown;
				return currentState.HandledState;
			}
		}

		public Events CurrentEvent
		{
			get
			{
				if (currentState == null) return Events.Unknown;
				return currentEvent;
			}
		}

		public bool Is(States isState, Events isEvent) { return isState == CurrentState && isEvent == CurrentEvent; }

		public IState CurrentHandler { get { return currentState; } }

		public StateMachine(Heartbeat heartbeat, params IState[] states)
		{
			if (heartbeat == null) throw new ArgumentNullException("heartbeat");

			this.heartbeat = heartbeat;
			stateEntries = states;

			heartbeat.Update += Update;
		}

		void Update(float delta)
		{
			entries.AddRange(queued);
			queued.Clear();
			var persisted = new List<IEntry>();
			var isBlocked = false;
			string blockingSynchronizedId = null;

			foreach (var entry in entries)
			{
				if (isBlocked && (string.IsNullOrEmpty(blockingSynchronizedId) || entry.SynchronizedId != blockingSynchronizedId))
				{
					blockingSynchronizedId = null;
					entry.EntryState = EntryStates.Blocked;
					persisted.Add(entry);
					continue;
				}

				if (entry.State != currentState.HandledState)
				{
					if (entry.State == nextState.HandledState)
					{
						entry.EntryState = EntryStates.Waiting;
						persisted.Add(entry);
					}
					continue;
				}

				var currentIsBlocking = false;

				entry.EntryState = EntryStates.Calling;
				if (entry.IsRepeating) persisted.Add(entry);
				try 
				{
					currentIsBlocking = !entry.Trigger();
				}
				catch (Exception exception) { Debug.LogException(exception); }
				finally
				{
					if (!entry.IsRepeating && currentIsBlocking)
					{
						blockingSynchronizedId = entry.SynchronizedId;
						entry.EntryState = EntryStates.Blocking;
						persisted.Add(entry);
					}
					isBlocked |= currentIsBlocking;
				}
			}
			entries = persisted;

			// We can't change states until we're unblocked.
			if (isBlocked) return;

			if (nextState.HandledState != currentState.HandledState)
			{
				if (currentEvent == Events.Idle)
				{
					// We transitioning states, but we haven't completed yet.
					SetState(currentState, Events.End, currentPayload);
				}
				else if (currentEvent == Events.End)
				{
					// We already completed last frame, so now we'll be starting.
					SetState(nextState, Events.Begin, nextPayload);
				}
			}
			else
			{
				if (currentEvent == Events.Begin)
				{
					// We already started last frame, so now we go to idle
					SetState(currentState, Events.Idle, currentPayload);
				}
			}
		}

		void SetState(IState state, Events stateEvent, object payload)
		{
			currentState = state;
			currentPayload = payload;
			currentEvent = stateEvent;

			currentState.UpdateState(currentState.HandledState, currentEvent, payload);
		}

		IState GetState<P>(P payload)
			where P : class, IStatePayload
		{
			foreach (var state in stateEntries)
			{
				if (state.AcceptsPayload(payload)) return state;
			}
			return null;
		}

		#region Pushing
		public void Push<T>(
			Action action,
			string description,
			bool repeating = false
		)
		{
			Push(
				action,
				typeof(T),
				description,
				repeating
			);
		}

		public void PushBlocking<T>(Action<Action> action, string description)
		{
			PushBlocking(
				action,
				typeof(T),
				description
			);
		}

		public void PushBlocking<T>(Action action, Func<bool> condition, string description)
		{
			PushBlocking(
				action,
				condition,
				typeof(T),
				description
			);
		}

		public void Push(
			Action action,
			Type type,
			string description,
			bool repeating = false,
			string synchronizedId = null
		)
		{
			OnPush(
				action,
				currentState.HandledState,
				currentEvent,
				type,
				description,
				repeating,
				synchronizedId
			);
		}

		public void PushBlocking(
			Action<Action> action,
			Type type,
			string description,
			string synchronizedId = null
		)
		{
			OnPushBlocking(
				action,
				currentState.HandledState,
				currentEvent,
				type,
				description,
				synchronizedId
			);
		}

		public void PushBlocking(
			Action action,
			Func<bool> condition,
			Type type,
			string description,
			string synchronizedId = null
		)
		{
			Action<Action> waiter = done =>
			{
				action();
				heartbeat.Wait(done, condition);
			};

			OnPushBlocking(
				waiter,
				currentState.HandledState,
				currentEvent,
				type,
				description,
				synchronizedId
			);
		}
		#endregion

		#region Push Handlers
		void OnPush(
			Action action,
			States state,
			Events stateEvent,
			Type type,
			string description,
			bool repeating,
			string synchronizedId
		)
		{
			if (action == null) throw new ArgumentNullException("action");
			if (state == States.Unknown) throw new ArgumentException("Cannot bind to States.Unknown");
			if (stateEvent == Events.Unknown) throw new ArgumentException("Cannot bind to Events.Unknown");
			if (string.IsNullOrEmpty(description)) throw new ArgumentException("Cannot have empty or null description");

			queued.Add(
				new NonBlockingEntry(
					action,
					state,
					stateEvent,
					type == null ? "< Unspecified >." + description : type.Name + "." + description,
					repeating,
					synchronizedId
				)
				{
					EntryState = EntryStates.Queued
				}
			);
		}

		void OnPushBlocking(
			Action<Action> action,
			States state,
			Events stateEvent,
			Type type,
			string description,
			string synchronizedId
		)
		{
			if (action == null) throw new ArgumentNullException("action");
			if (state == States.Unknown) throw new ArgumentException("Cannot bind to States.Unknown");
			if (stateEvent == Events.Unknown) throw new ArgumentException("Cannot bind to Events.Unknown");
			if (string.IsNullOrEmpty(description)) throw new ArgumentException("Cannot have empty or null description");

			queued.Add(
				new BlockingEntry(
					action, state,
					stateEvent,
					type == null ? "< Unspecified >." + description : type.Name + "." + description,
					synchronizedId
				)
				{
					EntryState = EntryStates.Queued
				}
			);
		}
		#endregion

		/// <summary>
		/// Requests to transition to the first state that accepts the specified payload.
		/// </summary>
		/// <param name="payload">Payload.</param>
		/// <typeparam name="P">Payload type.</typeparam>
		public void RequestState<P>(P payload = null)
			where P : class, IStatePayload, new()
		{
			payload = payload ?? new P();
			var handlingState = GetState(payload);
			if (handlingState == null) throw new Exception("Cannot find a handler for payload of type " + typeof(P));
			var state = handlingState.HandledState;

			if (state == States.Unknown) throw new ArgumentException("Cannot switch to an Unknown state");
			if (currentState != null && nextState != null)
			{
				if (currentState.HandledState == state || nextState.HandledState == state) return;
				if (currentState.HandledState != nextState.HandledState) throw new NotImplementedException("Cannot switch to another state while already transitioning");
			}

			nextState = handlingState;
			nextPayload = payload;
			nextState.Initialize(payload);
			if (currentState == null) SetState(nextState, Events.Begin, payload);
		}


		#region Utility
		public void PushBreak()
		{
			Push<StateMachine>(OnBreak, "PushBreak");
		}

		void OnBreak()
		{
			Debug.LogWarning("Break Pushed");
			Debug.Break();
		}

		public IEntryImmutable[] GetEntries() { return entries.Cast<IEntryImmutable>().ToArray(); }
		#endregion
	}
}