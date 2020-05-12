using System;
using System.Linq;
using Lunra.Hothouse.Ai;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public abstract class AgentPresenter<M, V, S> : PooledPresenter<M, V>
		where M : AgentModel
		where V : AgentView
		where S : AgentStateMachine<GameModel, M>, new()
	{
		protected S StateMachine;

		public AgentPresenter(GameModel game, M model) : base(game, model) { }

		protected override void Bind()
		{
			base.Bind();
			
			StateMachine = new S();
			
			View.InstanceName = typeof(V).Name + "_" + (string.IsNullOrEmpty(Model.Id.Value) ? "null_or_empty_id" : Model.Id.Value);

			Model.NavigationPlan.Value = NavigationPlan.Done(Model.Position.Value);
			
			Game.SimulationUpdate += OnGameSimulationUpdate;
			
			Model.Position.Changed += OnAgentPosition;
			Model.NavigationPlan.Changed += OnAgentNavigationPlan;
			Model.Health.Changed += OnAgentHealth;
		}

		protected override void UnBind()
		{
			base.UnBind();
			
			Game.SimulationUpdate -= OnGameSimulationUpdate;
			
			Model.Position.Changed -= OnAgentPosition;
			Model.NavigationPlan.Changed -= OnAgentNavigationPlan;
			Model.Health.Changed -= OnAgentHealth;
		}
		
		#region GameModel Events
		protected override void OnInitialized()
		{
			StateMachine.Initialize(Game, Model);
		}

		void OnGameSimulationUpdate()
		{
			switch (Model.PooledState.Value)
			{
				case PooledStates.InActive:
					return;
			}
			
			StateMachine.Update();
		}
		#endregion
		
		#region AgentModel Events
		protected virtual void OnAgentPosition(Vector3 position)
		{
			switch (Model.PooledState.Value)
			{
				case PooledStates.InActive:
					return;
			}

			View.RootTransform.position = position;
		}

		protected virtual void OnAgentNavigationPlan(NavigationPlan navigationPlan)
		{
			Model.Position.Value = navigationPlan.Position;
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

		protected virtual void OnAgentHealth(float health)
		{
			if (!Mathf.Approximately(0f, health)) return;

			if (!Model.Inventory.Value.IsEmpty)
			{
				Game.ItemDrops.Activate(
					m =>
					{
						m.Inventory.Value = Model.Inventory.Value;
						m.Job.Value = Jobs.None;
						m.RoomId.Value = Model.RoomId.Value;
						m.Position.Value = Model.Position.Value;
						m.Rotation.Value = Quaternion.identity;
					}
				);
			}

			if (Model.InventoryPromise.Value.Operation != InventoryPromise.Operations.Unknown)
			{
				var building = Game.Buildings.AllActive.FirstOrDefault(b => b.Id.Value == Model.InventoryPromise.Value.BuildingId);
				
				if (building == null) Debug.LogError("Cannot find an active building with id \"" + Model.InventoryPromise.Value.BuildingId + "\" to cancel out promise operation: " + Model.InventoryPromise.Value.Operation);
				else building.ConstructionInventoryPromised.Value -= Model.InventoryPromise.Value.Inventory;
			}
			
			Model.PooledState.Value = PooledStates.InActive;
		}
		#endregion
	}
}