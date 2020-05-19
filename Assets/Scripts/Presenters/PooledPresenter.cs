using System;
using Lunra.Hothouse.Models;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class PooledPresenter<M, V> : Presenter<V>
		where M : IPooledModel
		where V : class, IView
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
			Initialize();
			if (Game.IsSimulationInitialized) SimulationInitialize();
			else Game.SimulationInitialize += SimulationInitialize;
		}

		protected override void UnBind()
		{
			Model.PooledState.Changed -= OnPooledState;
			
			Game.SimulationInitialize -= SimulationInitialize;
		}
		
		void Initialize()
		{
			OnPooledState(Model.PooledState.Value);
			OnInitialized();
		}
		
		void SimulationInitialize()
		{
			OnSimulationInitialized();
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
			
			ShowView(Game.NavigationMesh.Root.Value, true);
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
		protected virtual void OnSimulationInitialized() { }
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

			if (QueueNavigationCalculation && Game.NavigationMesh.CalculationState.Value == NavigationMeshModel.CalculationStates.Completed)
			{
				Game.NavigationMesh.QueueCalculation();
			}
		}
		#endregion
		
		#region Utility
		protected virtual bool QueueNavigationCalculation => false;
		protected bool IsActive => Model.PooledState.Value == PooledStates.Active;
		protected bool IsNotActive => !IsActive;
		#endregion
	}
}