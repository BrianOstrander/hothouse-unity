using System;
using Lunra.Core;
using UnityEngine;

namespace Lunra.StyxMvp.Services 
{
	public enum WaitResults
	{
		Unknown = 0,
		Ready = 10,
		Cancel = 20
	}
	
	public class Heartbeat 
	{
		public event Action<float> Update = ActionExtensions.GetEmpty<float>();
		public event Action<float> LateUpdate = ActionExtensions.GetEmpty<float>();

		public void TriggerUpdate(float delta) => Update(delta);

		public void TriggerLateUpdate(float delta) => LateUpdate(delta);

		public void Wait(
			Action done,
			float seconds,
			bool checkInstantly = false
		)
		{
			if (done == null) throw new ArgumentNullException(nameof(done));
			if (seconds < 0f) throw new ArgumentOutOfRangeException(nameof(seconds), "Cannot be less than zero.");
			var endtime = DateTime.Now.AddSeconds(seconds);
			Wait(done, () => endtime < DateTime.Now, checkInstantly);
		}

		public void Wait(
			Action done,
			Func<bool> condition,
			bool checkInstantly = false
		)
		{
			if (done == null) throw new ArgumentNullException(nameof(done));
			if (condition == null) throw new ArgumentNullException(nameof(condition));

			Wait(status => done(), condition, checkInstantly);
		}

		/// <summary>
		/// Waits for the specified condition to be true and returns a cancel action.
		/// </summary>
		/// <param name="done">Done.</param>
		/// <param name="condition">Condition.</param>
		/// <param name="checkInstantly">Runs the event this frame, instead of waiting a frame for the first time it's called.</param>
		public Action Wait(
			Action<WaitResults> done,
			Func<bool> condition,
			bool checkInstantly = false
		)
		{
			if (done == null) throw new ArgumentNullException(nameof(done));
			if (condition == null) throw new ArgumentNullException(nameof(condition));

			var status = WaitResults.Unknown;
			void onCancel() => status = WaitResults.Cancel;

			void waiter(float delta)
			{
				try
				{
					if (status == WaitResults.Unknown)
					{
						if (condition()) status = WaitResults.Ready;
						else return;
					}
				}
				catch (Exception e)
				{
					Debug.LogException(e);
					return;
				}

				Update -= waiter; // This may give you a warning, it's safe to ignore since this lambda is local

				try
				{
					done(status);
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}

			Update += waiter;

			if (checkInstantly) waiter(0f);

			return onCancel;
		}
	}
}

