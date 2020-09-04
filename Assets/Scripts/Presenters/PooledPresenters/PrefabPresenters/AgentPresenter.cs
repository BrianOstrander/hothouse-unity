using System.Collections.Generic;
using Lunra.Hothouse.Ai;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public abstract class AgentPresenter<M, V, S> : PrefabPresenter<M, V>
		where M : AgentModel
		where V : AgentView
		where S : AgentStateMachine<GameModel, M>, new()
	{
		protected S StateMachine;

		public AgentPresenter(GameModel game, M model) : base(game, model) { }

		protected override void Bind()
		{
			StateMachine = new S();
			Model.StateMachine = StateMachine;
			
			View.Name = typeof(V).Name + "_" + (string.IsNullOrEmpty(Model.Id.Value) ? "null_or_empty_id" : Model.Id.Value);

			Model.NavigationPlan.Value = NavigationPlan.Done(Model.Transform.Position.Value);
			
			Game.SimulationUpdate += OnGameSimulationUpdate;

			Model.NavigationPlan.Changed += OnAgentNavigationPlan;
			Model.Health.Destroyed += OnAgentHealthDestroyed;
			Model.ObligationPromises.Complete += OnAgentObligationComplete;
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Game.SimulationUpdate -= OnGameSimulationUpdate;
			
			Model.NavigationPlan.Changed -= OnAgentNavigationPlan;
			Model.Health.Destroyed -= OnAgentHealthDestroyed;
			Model.ObligationPromises.Complete -= OnAgentObligationComplete;
			
			base.UnBind();
		}

		#region ViewEvents
		protected override void OnViewPrepare()
		{
			base.OnViewPrepare();
			
			View.RoomChanged += OnViewRoomChanged;
		}

		protected virtual void OnViewRoomChanged(string roomId)
		{
			Model.RoomTransform.Id.Value = roomId;
		}
		#endregion
		
		#region GameModel Events
		protected override void OnSimulationInitialized()
		{
			StateMachine.Initialize(Game, Model);
		}

		void OnGameSimulationUpdate()
		{
			if (View.NotVisible) return;
			
			StateMachine.Update();
		}
		#endregion
		
		#region AgentModel Events
		protected virtual void OnAgentNavigationPlan(NavigationPlan navigationPlan)
		{
			Model.Transform.Position.Value = navigationPlan.Position;
			if (navigationPlan.Normal.HasValue) Model.Transform.Rotation.Value = Quaternion.LookRotation(navigationPlan.Normal.Value);
		}

		protected override void OnPooledState(PooledStates pooledState)
		{
			switch (pooledState)
			{
				case PooledStates.InActive:
					StateMachine.Reset();		
					break;
			}
			
			base.OnPooledState(pooledState);
		}

		public virtual void OnAgentHealthDestroyed(Damage.Result result)
		{
			if (!string.IsNullOrEmpty(View.DeathEffectId))
			{
				Game.Effects.Queued.Enqueue(
					new EffectsModel.Request(
						Model.Transform.Position.Value,
						View.DeathEffectId
					)
				);
			}

			// TODO: Perhaps the component itself should check to see if it needs to break promises upon death...
			Model.ObligationPromises.BreakAllPromises();
		}

		public virtual void OnAgentObligationComplete(Obligation obligation) { }
		#endregion
	}
}