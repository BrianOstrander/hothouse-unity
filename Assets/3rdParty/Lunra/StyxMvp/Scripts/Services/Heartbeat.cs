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
		public ulong UpdateCount { get; private set; } = ulong.MinValue;
		public ulong FixedUpdateCount { get; private set; } = ulong.MinValue;
		
		public event Action Update = ActionExtensions.Empty;
		public event Action LateUpdate = ActionExtensions.Empty;
		public event Action FixedUpdate = ActionExtensions.Empty;

		public event Action<Action> DrawGizmos = ActionExtensions.GetEmpty<Action>();

		public void TriggerUpdate()
		{
			Update();
			UpdateCount++;
		}

		public void TriggerLateUpdate() => LateUpdate();
		
		public void TriggerFixedUpdate()
		{
			FixedUpdate();
			FixedUpdateCount++;
		}

		public void TriggerDrawGizmos()
		{
			DrawGizmos(
				() =>
				{
					Gizmos.color = Color.white;
					Gizmos.matrix = Matrix4x4.identity;
				}
			);	
		}

		public void WaitForSeconds(
			Action done,
			float seconds,
			bool checkInstantly = false
		)
		{
			if (seconds < 0f) throw new ArgumentOutOfRangeException(nameof(seconds), "Cannot be less than zero.");
			var endtime = DateTime.Now.AddSeconds(seconds);
			WaitForCondition(done, () => endtime < DateTime.Now, checkInstantly);
		}

		public void WaitForUpdate(Action done)
		{
			var endCount = UpdateCount + 1;
			WaitForCondition(
				done,
				() => endCount <= UpdateCount
			);
		}
		
		public void WaitForFixedUpdate(Action done)
		{
			var endCount = FixedUpdateCount + 1;
			WaitForCondition(
				done,
				() => endCount<= FixedUpdateCount
			);
		}

		public void WaitForCondition(
			Action done,
			Func<bool> condition,
			bool checkInstantly = false
		)
		{
			if (done == null) throw new ArgumentNullException(nameof(done));
			if (condition == null) throw new ArgumentNullException(nameof(condition));

			WaitForCondition(status => done(), condition, checkInstantly);
		}

		/// <summary>
		/// Waits for the specified condition to be true and returns a cancel action.
		/// </summary>
		/// <param name="done">Done.</param>
		/// <param name="condition">Condition.</param>
		/// <param name="checkInstantly">Runs the event this frame, instead of waiting a frame for the first time it's called.</param>
		public Action WaitForCondition(
			Action<WaitResults> done,
			Func<bool> condition,
			bool checkInstantly = false
		)
		{
			if (done == null) throw new ArgumentNullException(nameof(done));
			if (condition == null) throw new ArgumentNullException(nameof(condition));

			var status = WaitResults.Unknown;
			void onCancel() => status = WaitResults.Cancel;

			void waiter()
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

			if (checkInstantly) waiter();

			return onCancel;
		}
	}
}

