using System;
using UnityEngine;
using Lunra.Core;

namespace Lunra.StyxMvp.Services
{
	public class StartupPayload : IStatePayload
	{
		public Action Idle;
	}

	/// <summary>
	/// Used by StyxMvp to initialize everything it needs to do.
	/// </summary>
	/// <remarks>
	/// Since most games will have their own initialize state, I decided against naming this the same thing to save on
	/// confusion.
	/// </remarks>
	public class StartupState : State<StartupPayload>
	{
		// Reminder: Keep variables in payload for easy reset of states!

		protected override void Begin()
		{
			App.S.PushBlocking(InitializeModels);
			App.S.PushBlocking(InitializeViews);
			App.S.PushBlocking(InitializePresenters);
			App.S.PushBlocking(InitializeAudio);
		}

		protected override void Idle()
		{
			if (Payload.Idle == null)
			{
				Debug.LogError("No Idle was provided. Now stuck.");
				return;
			}

			Payload.Idle();
		}

		#region Mediators
		void InitializeModels(Action done)
		{
			App.M.Initialize(
				result =>
				{
					if (result.Status == ResultStatus.Success) done();
					else App.Restart("Initializing ModelMediator failed with status " + result.Status);
				}
			);
		}

		void InitializeViews(Action done)
		{
			App.V.Initialize(
				result =>
				{
					if (result.Status == ResultStatus.Success) done();
					else App.Restart("Initializing ViewMediator failed with status " + result.Status);
				}
			);
		}

		void InitializePresenters(Action done)
		{
			App.P.Initialize(
				result =>
				{
					if (result.Status == ResultStatus.Success) done();
					else App.Restart("Initializing PresenterMediator failed with status " + result.Status);
				}
			);
		}
		#endregion

		void InitializeAudio(Action done)
		{
			App.Audio.Initialize(
				result =>
				{
					if (result.Status == ResultStatus.Success) done(); 
					else App.Restart("Initializing Audio failed with status " + result.Status);
				}
			);
		}
	}
}