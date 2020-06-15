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
			Model.Transform.Position.Changed += OnPosition;
			Model.Transform.Rotation.Changed += OnRotation;
			
			Initialize();
			
			if (Game.IsSimulationInitialized) SimulationInitialize();
			else Game.SimulationInitialize += SimulationInitialize;
		}

		protected override void UnBind()
		{
			Model.PooledState.Changed -= OnPooledState;
			Model.Transform.Position.Changed -= OnPosition;
			Model.Transform.Rotation.Changed -= OnRotation;
			
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
		
		protected void Show()
		{
			if (View.Visible) return;
			
			View.Reset();

			View.Prepare += ViewSetTransform;
			View.Prepare += OnViewPrepare;

			View.Shown += OnViewShown;
			
			View.PrepareClose += OnViewPrepareClose;
			View.Closed += OnViewClosed;
			
			ShowView(Game.NavigationMesh.Root.Value, true);
		}

		protected void Close()
		{
			if (View.NotVisible) return;
			
			CloseView(true);
		}

		void ViewSetTransform()
		{
			OnPosition(Model.Transform.Position.Value);
			OnRotation(Model.Transform.Rotation.Value);
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
					if (CanShow()) Show();
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

		protected virtual bool CanShow() => true;
		#endregion
		
		#region ITransform Events
		protected virtual void OnPosition(Vector3 position)
		{
			if (IsNotActive || View.NotVisible) return;

			View.RootTransform.position = position;
		}
		
		protected virtual void OnRotation(Quaternion rotation)
		{
			if (IsNotActive || View.NotVisible) return;
			
			View.RootTransform.rotation = rotation;
		}
		#endregion
		
		#region Utility
		protected virtual bool QueueNavigationCalculation => false;
		protected bool IsActive => Model.PooledState.Value == PooledStates.Active;
		protected bool IsNotActive => !IsActive;
		#endregion
	}
}