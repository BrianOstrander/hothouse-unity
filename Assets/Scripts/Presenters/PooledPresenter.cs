using System;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;
using Lunra.WildVacuum.Models;
using UnityEngine;

namespace Lunra.WildVacuum.Presenters
{
	public class PooledPresenter<M, V> : Presenter<V>
		where M : PooledModel
		where V : View
	{
		protected readonly GameModel Game;
		protected M Model;

		public PooledPresenter(
			GameModel game,
			M model
		) : this(
			game,
			model,
			App.V.Get<V>()
		)
		{ }

		public PooledPresenter(
			GameModel game,
			M model,
			V view
		) : base(
			view
		)
		{
			Game = game;
			Model = model;

			Model.HasPresenter.Value = true;

			if (string.IsNullOrEmpty(Model.Id.Value)) Model.Id.Value = Guid.NewGuid().ToString();
			
			Bind();
		}

		protected virtual void Bind()
		{
			Model.PooledState.Changed += OnPooledState;
			if (Game.IsSimulationInitialized) Initialize();
			else Game.SimulationInitialize += Initialize;
		}

		protected override void UnBind()
		{
			Model.PooledState.Changed -= OnPooledState;
			
			Game.SimulationInitialize -= Initialize;
		}
		
		void Initialize()
		{
			OnPooledState(Model.PooledState.Value);
			OnInitialized();
		}
		
		void Show()
		{
			if (View.Visible) return;
			
			View.Reset();

			View.Prepare += OnViewPrepare;

			View.Shown += ViewSetTransform;
			View.Shown += OnViewShown;
			
			View.PrepareClose += OnViewPrepareClose;
			View.Closed += OnViewClosed;
			
			ShowView(instant: true);
		}

		void Close()
		{
			if (View.NotVisible) return;
			
			CloseView(true);
		}

		void ViewSetTransform()
		{
			View.RootTransform.position = Model.Position.Value;
			View.RootTransform.rotation = Model.Rotation.Value;
		}
		
		#region Events
		protected virtual void OnInitialized() { }
		#endregion
		
		#region View Event
		protected virtual void OnViewPrepare() { }
		protected virtual void OnViewShown() { }
		protected virtual void OnViewPrepareClose() { }
		protected virtual void OnViewClosed() { }
		#endregion

		#region PooledModel Events
		protected virtual void OnPooledState(PooledStates pooledState)
		{
			switch (pooledState)
			{
				case PooledStates.InActive:
					Close();
					break;
				case PooledStates.Active:
					Show();
					break;
				default:
					Debug.LogError("Unrecognized state: " + pooledState);
					break;
			}
		}
		#endregion
	}
}