using System;
using Lunra.StyxMvp;
using Lunra.WildVacuum.Ai;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;
using UnityEngine;

namespace Lunra.WildVacuum.Presenters
{
	public class AgentPresenter<V, M, S> : PooledPresenter<V, M>
		where V : AgentView
		where M : AgentModel
		where S : AgentStateMachine<GameModel, M>, new()
	{
		protected S StateMachine;

		public AgentPresenter(GameModel game, M model) : base(game, model) { }

		protected override void OnBind()
		{
			base.OnBind();
			
			StateMachine = new S();
			
			View.InstanceName = typeof(V).Name + "_" + (string.IsNullOrEmpty(Model.Id.Value) ? "null_or_empty_id" : Model.Id.Value);

			App.Heartbeat.DrawGizmos += OnHeartbeatDrawGizmos;
			
			Model.NavigationPlan.Value = NavigationPlan.Done(Model.Position.Value);
			
			Game.SimulationUpdate += OnGameSimulationUpdate;
			
			Model.Position.Changed += OnAgentPosition;
			Model.NavigationPlan.Changed += OnAgentNavigationPlan;
		}

		protected override void OnUnBind()
		{
			base.OnUnBind();
			
			App.Heartbeat.DrawGizmos -= OnHeartbeatDrawGizmos;
			
			Game.SimulationUpdate -= OnGameSimulationUpdate;
			
			Model.Position.Changed -= OnAgentPosition;
			Model.NavigationPlan.Changed -= OnAgentNavigationPlan;
		}

		#region Heartbeat Events
		protected virtual void OnHeartbeatDrawGizmos(Action cleanup)
		{
			if (!Model.IsDebugging) return;
			
			switch (Model.NavigationPlan.Value.State)
			{
				case NavigationPlan.States.Navigating:
					Gizmos.color = Color.green;
			
					for (var i = 1; i < Model.NavigationPlan.Value.Nodes.Length; i++)
					{
						Gizmos.DrawLine(
							Model.NavigationPlan.Value.Nodes[i - 1],
							Model.NavigationPlan.Value.Nodes[i]
						);
					}
					break;
				case NavigationPlan.States.Invalid:
					Gizmos.color = Color.red;
					
					Gizmos.DrawLine(
						Model.NavigationPlan.Value.Position,
						Model.NavigationPlan.Value.EndPosition
					);
					break;
			}

			cleanup();
		}
		#endregion
		
		#region GameModel Events
		protected override void OnInitialized()
		{
			base.OnInitialized();
			
			StateMachine.Initialize(Game, Model);
		}

		void OnGameSimulationUpdate(float delta)
		{
			switch (Model.PooledState.Value)
			{
				case PooledStates.Pooled:
					return;
			}
			
			StateMachine.Update(delta);
		}
		#endregion
		
		#region AgentModel Events
		protected virtual void OnAgentPosition(Vector3 position)
		{
			switch (Model.PooledState.Value)
			{
				case PooledStates.Pooled:
				case PooledStates.NotVisible:
					return;
			}

			View.RootTransform.position = position;
		}

		protected virtual void OnAgentNavigationPlan(NavigationPlan navigationPlan)
		{
			Model.Position.Value = navigationPlan.Position;
		}
		#endregion
	}
}