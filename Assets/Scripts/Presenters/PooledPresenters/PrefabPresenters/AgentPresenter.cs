using System;
using System.Linq;
using Lunra.Hothouse.Ai;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;
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
			
			View.InstanceName = typeof(V).Name + "_" + (string.IsNullOrEmpty(Model.Id.Value) ? "null_or_empty_id" : Model.Id.Value);

			Model.NavigationPlan.Value = NavigationPlan.Done(Model.Position.Value);
			
			Game.SimulationUpdate += OnGameSimulationUpdate;
			
			Model.Position.Changed += OnAgentPosition;
			Model.NavigationPlan.Changed += OnAgentNavigationPlan;
			Model.Health.Changed += OnAgentHealth;
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Game.SimulationUpdate -= OnGameSimulationUpdate;
			
			Model.Position.Changed -= OnAgentPosition;
			Model.NavigationPlan.Changed -= OnAgentNavigationPlan;
			Model.Health.Changed -= OnAgentHealth;
			
			base.UnBind();
		}
		
		#region GameModel Events
		protected override void OnSimulationInitialized()
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
					"default",
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

			switch (Model.InventoryPromise.Value.Operation)
			{
				case InventoryPromise.Operations.None: break;
				case InventoryPromise.Operations.ConstructionDeposit:
					var building = Game.Buildings.FirstOrDefaultActive(Model.InventoryPromise.Value.TargetId);
				
					if (building == null) Debug.LogError("Cannot find an active building with id \"" + Model.InventoryPromise.Value.TargetId + "\" to cancel out promise operation: " + Model.InventoryPromise.Value.Operation+", this should never happen");
					else building.ConstructionInventoryPromised.Value -= Model.InventoryPromise.Value.Inventory;
					
					break;
				case InventoryPromise.Operations.CleanupWithdrawal:
					var itemDrop = Game.ItemDrops.FirstOrDefaultActive(Model.InventoryPromise.Value.TargetId);

					if (itemDrop == null) Debug.LogError("Cannot find an active itemDrop with id \"" + Model.InventoryPromise.Value.TargetId + "\" to cancel out operation: " + Model.InventoryPromise.Value.Operation + ", this should never happen");
					else itemDrop.WithdrawalInventoryPromised.Value -= Model.InventoryPromise.Value.Inventory;
					
					break;
				default:
					Debug.LogError("Unrecognized operation: " + Model.InventoryPromise.Value.Operation);
					break;
			}
			
			Model.PooledState.Value = PooledStates.InActive;
		}
		#endregion
	}
}