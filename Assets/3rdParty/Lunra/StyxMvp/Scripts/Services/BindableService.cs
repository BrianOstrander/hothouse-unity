using System;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Models;
using Lunra.StyxMvp.Services;
using UnityEngine;

namespace Lunra.StyxMvp.Services
{
	public abstract class BindableService<M>
		where M : IModel
	{
		protected M Model { get; private set; }
		protected bool IsInitialized { get; private set; }

		public BindableService(M model)
		{
			Model = model;

			App.S.StateChange += OnState;

			if (App.S.CurrentEvent == StateMachine.Events.Idle)
			{
				IsInitialized = true;
				Bind();
			}
		}

		protected virtual void Bind() { }

		protected virtual void UnBind() { }
		
		#region Events
		void OnState(StateChange state)
		{
			switch (state.Event)
			{
				case StateMachine.Events.Begin:
					break;
				case StateMachine.Events.Idle:
					if (!IsInitialized)
					{
						IsInitialized = true;
						Bind();
					}
					break;
				case StateMachine.Events.End:
					UnBind();
					App.S.StateChange -= OnState;
					break;
				default:
					Debug.LogError("Unrecognized Event: "+state.Event);
					break;
			}
		}
		#endregion
	}
}