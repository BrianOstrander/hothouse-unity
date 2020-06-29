using System;
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
			
			View.Name = typeof(V).Name + "_" + (string.IsNullOrEmpty(Model.Id.Value) ? "null_or_empty_id" : Model.Id.Value);

			Model.NavigationPlan.Value = NavigationPlan.Done(Model.Transform.Position.Value);
			
			Game.SimulationUpdate += OnGameSimulationUpdate;

			Model.Transform.Position.Changed += OnAgentPosition;
			Model.NavigationPlan.Changed += OnAgentNavigationPlan;
			Model.Health.Current.Changed += OnAgentHealthCurrent;
			Model.Health.Damaged += OnAgentHealthDamaged;
			Model.ObligationComplete += OnAgentObligationComplete;
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Game.SimulationUpdate -= OnGameSimulationUpdate;
			
			Model.Transform.Position.Changed -= OnAgentPosition;
			Model.NavigationPlan.Changed -= OnAgentNavigationPlan;
			Model.Health.Current.Changed -= OnAgentHealthCurrent;
			Model.Health.Damaged -= OnAgentHealthDamaged;
			Model.ObligationComplete -= OnAgentObligationComplete;
			
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
		protected virtual void OnAgentPosition(Vector3 position)
		{
			View.RootTransform.position = position;
		}

		protected virtual void OnAgentNavigationPlan(NavigationPlan navigationPlan)
		{
			Model.Transform.Position.Value = navigationPlan.Position;
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

		protected virtual void OnAgentHealthCurrent(float health)
		{
			if (!Mathf.Approximately(0f, health)) return;

			if (!Model.Inventory.All.Value.IsEmpty)
			{
				Game.ItemDrops.Activate(
					"default",
					Model.RoomTransform.Id.Value,
					Model.Transform.Position.Value,
					Quaternion.identity,
					m =>
					{
						m.Inventory.Add(Model.Inventory.All.Value);
						m.Job.Value = Jobs.None;
						m.Transform.Position.Value = Model.Transform.Position.Value;
						m.Transform.Rotation.Value = Quaternion.identity;
					}
				);
			}

			switch (Model.InventoryPromise.Value.Operation)
			{
				case InventoryPromiseOld.Operations.None: break;
				case InventoryPromiseOld.Operations.ConstructionDeposit:
					if (Model.InventoryPromise.Value.Target.TryGetInstance<IConstructionModel>(Game, out var constructionDepositTarget))
					{
						constructionDepositTarget.ConstructionInventory.RemoveReserved(Model.InventoryPromise.Value.Inventory);
					}
					
					var constructionInventory = Model.InventoryPromise.Value.Inventory - Model.Inventory.All.Value;
					if (!constructionInventory.IsEmpty)
					{
						if (Model.InventoryPromise.Value.Source.TryGetInstance<IInventoryModel>(Game, out var constructionDepositSource))
						{
							constructionDepositSource.Inventory.RemoveForbidden(constructionInventory);
						}
					}
					break;
				case InventoryPromiseOld.Operations.CleanupWithdrawal:
					if (Model.InventoryPromise.Value.Target.TryGetInstance<IInventoryModel>(Game, out var cleanupNextTarget))
					{
						cleanupNextTarget.Inventory.RemoveReserved(Model.InventoryPromise.Value.Inventory); 
					}
					
					if (Model.InventoryPromise.Value.Source.TryGetInstance<IInventoryModel>(Game, out var cleanupNextSource))
					{
						cleanupNextSource.Inventory.RemoveForbidden(Model.InventoryPromise.Value.Inventory); 
					}
					break;
				default:
					Debug.LogError("Unrecognized operation: " + Model.InventoryPromise.Value.Operation);
					break;
			}

			Debug.LogWarning("Handle unfulfilled obligation promises here!");
			
			Model.PooledState.Value = PooledStates.InActive;
		}

		public virtual void OnAgentHealthDamaged(Damage.Result result)
		{
			if (result.IsTargetDestroyed)
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
			}
		}

		public virtual void OnAgentObligationComplete(Obligation obligation) { }
		#endregion
	}
}