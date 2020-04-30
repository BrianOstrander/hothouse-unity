using Lunra.StyxMvp.Presenters;
using Lunra.WildVacuum.Ai;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;
using UnityEngine;

namespace Lunra.WildVacuum.Presenters
{
	public class AgentPresenter<V, M, S> : Presenter<V>
		where M : AgentModel
		where V : AgentView
		where S : AgentStateMachine<GameModel, M>, new()
	{
		protected GameModel Game;
		protected M Agent;
		protected S StateMachine;

		public AgentPresenter(
			GameModel game,
			M agent
		)
		{
			Game = game;
			Agent = agent;
			StateMachine = new S();

			OnBind();
		}

		protected virtual void OnBind()
		{
			View.InstanceName = typeof(V).Name + "_" + (string.IsNullOrEmpty(Agent.Id.Value) ? "null_or_empty_id" : Agent.Id.Value);
			
			Game.SimulationUpdate += OnGameSimulationUpdate;
			
			Agent.State.Changed += OnAgentState;
			Agent.Position.Changed += OnAgentPosition;
			Agent.NavigationPlan.Changed += OnAgentNavigationPlan;
			
			if (Game.IsSimulationInitialized) OnInitialize();
			else Game.SimulationInitialize += OnInitialize;	
		}

		protected override void OnUnBind()
		{
			Game.SimulationUpdate -= OnGameSimulationUpdate;
			
			Agent.State.Changed -= OnAgentState;
			Agent.Position.Changed -= OnAgentPosition;
			Agent.NavigationPlan.Changed -= OnAgentNavigationPlan;
			
			Game.SimulationInitialize -= OnInitialize;
		}
		
		protected virtual void Show()
		{
			if (View.Visible) return;
			
			View.Reset();

			ShowView(instant: true);

			View.RootTransform.position = Agent.Position.Value;
			View.RootTransform.rotation = Agent.Rotation.Value;
		}

		protected virtual void Close()
		{
			if (View.NotVisible) return;
			
			CloseView(true);
		}
		
		#region GameModel Events
		protected virtual void OnInitialize()
		{
			StateMachine.Initialize(Game, Agent);
			
			OnAgentState(Agent.State.Value);
		}

		void OnGameSimulationUpdate(float delta)
		{
			switch (Agent.State.Value)
			{
				case AgentModel.States.Pooled:
					return;
			}
			
			StateMachine.Update(delta);
		}
		#endregion
		
		#region AgentModel Events
		protected virtual void OnAgentState(AgentModel.States state)
		{
			switch (state)
			{
				case AgentModel.States.Pooled:
				case AgentModel.States.NotVisible:
					Close();
					break;
				case AgentModel.States.Visible:
					Show();
					break;
				default:
					Debug.LogError("Unrecognized state: "+state);
					break;
			}
		}

		protected virtual void OnAgentPosition(Vector3 position)
		{
			switch (Agent.State.Value)
			{
				case AgentModel.States.Pooled:
				case AgentModel.States.NotVisible:
					return;
			}

			View.RootTransform.position = position;
		}

		protected virtual void OnAgentNavigationPlan(NavigationPlan navigationPlan)
		{
			Agent.Position.Value = navigationPlan.Position;
		}
		#endregion
	}
}