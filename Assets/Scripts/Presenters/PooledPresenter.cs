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

			Model.InitializeComponents(Game);
		
			Model.PooledState.Changed += OnPooledState;
			
			OnPooledState(Model.PooledState.Value);
		}

		protected override void Deconstruct()
		{
			Model.PooledState.Changed -= OnPooledState;
			
			if (Model.PooledState.Value != PooledStates.InActive) UnBind();
		}

		protected virtual void Bind()
		{
			Model.Transform.Position.Changed += OnPosition;
			Model.Transform.Rotation.Changed += OnRotation;

			Model.BindComponents();
			
			if (Game.IsSimulationInitialized) SimulationInitialize();
			else Game.SimulationInitialize += SimulationInitialize;
		}

		protected virtual void UnBind()
		{
			Model.Transform.Position.Changed -= OnPosition;
			Model.Transform.Rotation.Changed -= OnRotation;
			
			Model.UnBindComponents();
			
			Game.SimulationInitialize -= SimulationInitialize;
		}

		void SimulationInitialize()
		{
			OnSimulationInitialized();
		}
		
		protected void Show()
		{
			if (View.Visible) return;
			
			View.Cleanup();

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
					UnBind();
					break;
				case PooledStates.Active:
					Bind();
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
			if (View.NotVisible) return;

			View.RootTransform.position = position;
		}
		
		protected virtual void OnRotation(Quaternion rotation)
		{
			if (View.NotVisible) return;
			
			View.RootTransform.rotation = rotation;
		}
		#endregion
		
		#region Utility
		protected virtual bool QueueNavigationCalculation => false;
		#endregion
	}
}