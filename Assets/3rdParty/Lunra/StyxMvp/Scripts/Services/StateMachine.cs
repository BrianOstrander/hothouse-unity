using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Lunra.Core;
using Debug = UnityEngine.Debug;

namespace Lunra.StyxMvp.Services
{
	public class StateMachine
	{
		public static PlayerPrefsBool CaptureTraceData = new PlayerPrefsBool("LunraStyxMvp_StateMachine_CaptureTraceData");
		public static PlayerPrefsBool LogStateChanges = new PlayerPrefsBool("LunraStyxMvp_StateMachine_LogStateChanges");
		
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

		public struct TraceData
		{
			static string FormatMethodName(string methodName) => methodName.Remove(0, methodName.IndexOf(' ') + 1);
			
			public readonly bool IsValid;
			public readonly bool IsBlocking;
			
			public readonly string InitializerClassName;
			public readonly string InitializerMethodName;
			public readonly string InitializerFilePath;
			public readonly int InitializerFileLine;

			public readonly bool IsCallbackAnonymous;
			
			public readonly string CallbackClassName;
			public readonly string CallbackMethodName;
			
			public TraceData(
				StackFrame frame,
				MethodInfo callbackInfo,
				bool isBlocking
			)
			{
				IsValid = true;
				IsBlocking = isBlocking;
				
				InitializerClassName = frame.GetMethod().DeclaringType?.FullName;
				InitializerMethodName = FormatMethodName(frame.GetMethod().ToString());
				InitializerFilePath = frame.GetFileName();
				InitializerFileLine = frame.GetFileLineNumber();
				
				CallbackClassName = callbackInfo.DeclaringType?.FullName;
				CallbackMethodName = FormatMethodName(callbackInfo.ToString());

				IsCallbackAnonymous = callbackInfo.Name.Any((new [] {'<', '>'}).Contains);
			}

			public string GetReadableInitializer()
			{
				if (IsValid) return InitializerClassName + ":" + InitializerMethodName;
				return "N / A";
			}

			public string GetReadableCallback()
			{
				if (IsValid) return CallbackClassName + ":" + CallbackMethodName;
				return "N / A";
			}

			public string GetReadableName()
			{
				if (IsValid) return IsCallbackAnonymous ? "<b>< Anonymous ></b>" : "<b>" + CallbackMethodName + "</b> <i>" + CallbackClassName + "</i>";
				return "<b>Unknown</b> (Inspecting was not enabled when this was captured)";
			}
		}

		/// <summary>
		/// Used for external examination and debugging of the StateMachine.
		/// </summary>
		public interface IEntryImmutable
		{
			Type State { get; }
			Events Event { get; }
			TraceData Trace { get; }
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
			public Type State { get; protected set; }
			public Events Event { get; protected set; }
			public TraceData Trace { get; protected set; }
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
				Type state,
				Events stateEvent,
				TraceData trace,
				string synchronizedId
			)
			{
				this.action = action;
				State = state;
				Event = stateEvent;
				Trace = trace;
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
				// ReSharper disable once PossibleInvalidOperationException
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
				Type state,
				Events stateEvent,
				TraceData trace,
				bool repeating,
				string synchronizedId
			)
			{
				this.action = action;
				State = state;
				Event = stateEvent;
				Trace = trace;
				IsRepeating = repeating;
				SynchronizedId = string.IsNullOrEmpty(synchronizedId) ? null : synchronizedId;
			}

			public override bool Trigger()
			{
				action();
				return true;
			}
		}

		#region Callbacks
		// TODO: Pass this off to instantiated states so they can call this event without keeping this public...
		/// <summary>
		/// Called when a state change happens.
		/// </summary>
		/// <remarks>
		/// Do not call this to request a state change.
		/// </remarks>
		public Action<StateChange> StateChange = ActionExtensions.GetEmpty<StateChange>();
		#endregion
		
		Heartbeat heartbeat;

		IState[] stateEntries;

		IState currentState;
		object currentPayload;
		IState nextState;
		object nextPayload;
		Events currentEvent;
		List<IEntry> queued = new List<IEntry>();
		List<IEntry> entries = new List<IEntry>();

		public Type CurrentState => currentState?.GetType();

		public Events CurrentEvent
		{
			get
			{
				if (currentState == null) return Events.Unknown;
				return currentEvent;
			}
		}

		public bool Is(Type isState, Events isEvent) { return isState == CurrentState && isEvent == CurrentEvent; }

		public IState CurrentHandler => currentState;

		public StateMachine(Heartbeat heartbeat, params IState[] states)
		{
			this.heartbeat = heartbeat ?? throw new ArgumentNullException(nameof(heartbeat));
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

				if (entry.State != currentState.GetType())
				{
					if (entry.State == nextState.GetType())
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

			if (nextState.GetType() != currentState.GetType())
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

			currentState.UpdateState(currentState.GetType(), currentEvent, payload);
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
		public void Push(
			Action action,
			bool repeating = false,
			string synchronizedId = null
		)
		{
			OnPush(
				action,
				currentState.GetType(),
				currentEvent,
				GetTraceData(action.Method, false),
				repeating,
				synchronizedId
			);
		}

		public void PushBlocking(
			Action<Action> action,
			string synchronizedId = null
		)
		{
			OnPushBlocking(
				action,
				currentState.GetType(),
				currentEvent,
				GetTraceData(action.Method, true),
				synchronizedId
			);
		}

		public void PushBlocking(
			Action action,
			Func<bool> condition,
			string synchronizedId = null
		)
		{
			void waiter(Action done)
			{
				action();
				heartbeat.Wait(done, condition);
			}

			OnPushBlocking(
				waiter,
				currentState.GetType(),
				currentEvent,
				GetTraceData(action.Method, true),
				synchronizedId
			);
		}
		#endregion

		#region Push Handlers
		void OnPush(
			Action action,
			Type state,
			Events stateEvent,
			TraceData trace,
			bool repeating,
			string synchronizedId
		)
		{
			if (action == null) throw new ArgumentNullException(nameof(action));
			if (state == null) throw new ArgumentException("Cannot bind to null state");
			if (stateEvent == Events.Unknown) throw new ArgumentException("Cannot bind to Events.Unknown");

			queued.Add(
				new NonBlockingEntry(
					action,
					state,
					stateEvent,
					trace,
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
			Type state,
			Events stateEvent,
			TraceData trace,
			string synchronizedId
		)
		{
			if (action == null) throw new ArgumentNullException(nameof(action));
			if (state == null) throw new ArgumentException("Cannot bind to null state");
			if (stateEvent == Events.Unknown) throw new ArgumentException("Cannot bind to Events.Unknown");

			queued.Add(
				new BlockingEntry(
					action, state,
					stateEvent,
					trace,
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
			var state = handlingState.GetType();

			if (state == null) throw new ArgumentException("Cannot switch to a null state");
			if (currentState != null && nextState != null)
			{
				if (currentState.GetType() == state || nextState.GetType() == state) return;
				if (currentState.GetType() != nextState.GetType()) throw new Exception("Cannot switch to another state while already transitioning");
			}

			nextState = handlingState;
			nextPayload = payload;
			nextState.Initialize(payload);
			if (currentState == null) SetState(nextState, Events.Begin, payload);
		}


		#region Utility
		public void PushBreak()
		{
			Push(OnBreak);
		}

		void OnBreak()
		{
			Debug.LogWarning("Break Pushed");
			Debug.Break();
		}

		public IEntryImmutable[] GetEntries() { return entries.Cast<IEntryImmutable>().ToArray(); }
		
		TraceData GetTraceData(
			MethodInfo callbackInfo,
			bool isBlocking,
			int offset = 2
		)
		{
			if (CaptureTraceData.Value)
			{
				try
				{
					return new TraceData(
						new StackTrace(true).GetFrame(offset),
						callbackInfo,
						isBlocking
					);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
					return default;
				}
			}
			return default;
		}
		#endregion
	}
}