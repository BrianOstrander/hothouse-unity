using System;

using UnityEngine;

namespace LunraGames.SubLight
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
				Idle = () => App.SM.RequestState(nextPayload)
			};
		}

		/// <summary>
		/// Who requested this transition.
		/// </summary>
		public string Requester;
		public Action Idle;
	}

	public class TransitionState : State<TransitionPayload>
	{
		// Reminder: Keep variables in payload for easy reset of states!

		public override StateMachine.States HandledState { get { return StateMachine.States.Transition; } }

		#region Idle
		protected override void Idle()
		{
			if (Payload.Idle == null)
			{
				Debug.Log("Transition requested by " + (string.IsNullOrEmpty(Payload.Requester) ? "< null or empty >" : Payload.Requester)+", but no Idle was provided. Now stuck.");
				return;
			}

			Payload.Idle();
		}
		#endregion

		#region Events
		#endregion
	}
}