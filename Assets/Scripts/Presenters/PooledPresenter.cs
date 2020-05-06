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

			OnBind();
			
			if (Game.IsSimulationInitialized) OnInitialized();
			else Game.SimulationInitialize += OnInitialized;
		}

		protected virtual void OnBind()
		{
			Model.PooledState.Changed += OnPooledState;
		}

		protected override void OnUnBind()
		{
			Model.PooledState.Changed -= OnPooledState;
			
			Game.SimulationInitialize -= OnInitialized;
		}
		
		void Show()
		{
			if (View.Visible) return;
			
			View.Reset();
			
			
			OnShow();
			
			ShowView(instant: true);

			View.RootTransform.position = Model.Position.Value;
			View.RootTransform.rotation = Model.Rotation.Value;
		}

		void Close()
		{
			if (View.NotVisible) return;
			
			OnClose();
			
			CloseView(true);
		}
		
		#region Events
		protected virtual void OnInitialized()
		{
			OnPooledState(Model.PooledState.Value);
		}
		#endregion
		
		#region View Events
		protected virtual void OnShow() { }
		
		protected virtual void OnClose() { }
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