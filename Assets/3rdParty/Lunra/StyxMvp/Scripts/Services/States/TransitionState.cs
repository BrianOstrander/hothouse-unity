using System;

using UnityEngine;

namespace Lunra.StyxMvp.Services
{
	public class TransitionPayload : IStatePayload
	{
		public static TransitionPayload Quit(string requester)
		{
			return new TransitionPayload
			{
				Requester = requester,
				Idle = () =>
				{
					if (Application.isEditor) Debug.LogError("Quiting from the editor is not supported, you are now stuck here...");
					Application.Quit();
				}
			};
		}

		public static TransitionPayload Fallthrough<P>(
			string requester,
			P nextPayload
		)
			where P : class, IStatePayload, new()
		{
			return new TransitionPayload
			{
				Requester = requester,
				Idle = () => App.S.RequestState(nextPayload)
			};
		}

		/// <summary>
		/// Who requested this transition.
		/// </summary>
		public string Requester;
		/// <summary>
		/// Callback upon transition state reaching its idle.
		/// </summary>
		public Action Idle;
	}

	/// <summary>
	/// Used as a transitional state, perfect for exiting and returning back to an existing state so I don't have to
	/// handle same state transitions...
	/// </summary>
	public class TransitionState : State<TransitionPayload>
	{
		// Reminder: Keep variables in payload for easy reset of states!

		#region Idle
		protected override void Idle()
		{
			if (Payload.Idle == null)
			{
				Debug.LogError("Transition requested by " + (string.IsNullOrEmpty(Payload.Requester) ? "< null or empty >" : Payload.Requester)+", but no Idle was provided. Now stuck.");
				return;
			}

			Payload.Idle();
		}
		#endregion
	}
}