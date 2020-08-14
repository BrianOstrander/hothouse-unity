using System;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class ClearablePresenter<M, V> : PrefabPresenter<M, V>
		where M : IClearableModel
		where V : class, IClearableView
	{
		public ClearablePresenter(GameModel game, M model) : base(game, model) { }

		protected override void Bind()
		{			
			Model.Clearable.MeleeRangeBonus.Value = View.MeleeRangeBonus;
			
			Game.NavigationMesh.CalculationState.Changed += OnNavigationMeshCalculationState;
	
			Model.Obligations.All.Changed += OnObligationAll;
			Model.Obligations.Bind(
				ObligationCategories.Destroy.Melee,
				OnObligationDestroyMelee
			);
			Model.Health.Current.Changed += OnClearableHealthCurrent;
			Model.LightSensitive.LightLevel.Changed += OnLightSensitiveLightLevel;
			Model.Clearable.State.Changed += OnClearableState;
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Game.NavigationMesh.CalculationState.Changed -= OnNavigationMeshCalculationState;
			
			Model.Obligations.All.Changed -= OnObligationAll;
			Model.Obligations.UnBind(
				ObligationCategories.Destroy.Melee,
				OnObligationDestroyMelee
			);
			Model.Health.Current.Changed -= OnClearableHealthCurrent;
			Model.LightSensitive.LightLevel.Changed -= OnLightSensitiveLightLevel;
			Model.Clearable.State.Changed -= OnClearableState;
			
			base.UnBind();
		}

		protected virtual Inventory CalculateItemDrops() => Model.Clearable.ItemDrops.Value;

		protected override void OnViewPrepare()
		{
			base.OnViewPrepare();
			
			View.Shown += () => OnObligationAll(Model.Obligations.All.Value);
			
			Model.RecalculateEntrances(View);
		}

		#region ClearableModel Events
		void OnObligationAll(ObligationComponent.State state)
		{
			if (View.NotVisible) return;

			if (Model.Obligations.HasAny(ObligationCategories.Destroy.Melee))
			{
				View.Select();
			}
			else
			{
				View.Deselect();
			}
		}

		void OnClearableHealthCurrent(float health)
		{
			if (!Mathf.Approximately(0f, health)) return;

			if (!Model.Clearable.ItemDrops.Value.IsEmpty)
			{
				Game.ItemDrops.Activate(
					Model.RoomTransform.Id.Value,
					Model.Transform.Position.Value,
					Quaternion.identity,
					CalculateItemDrops()
				);
			}

			Model.PooledState.Value = PooledStates.InActive;
		}
		#endregion

		#region ClearableComponent Events
		void OnClearableState(ClearableStates state)
		{
			switch (state)
			{
				case ClearableStates.NotMarked:
					break;
				case ClearableStates.Highlighted:
					break;
				case ClearableStates.Marked:
					Model.Obligations.Add(ObligationCategories.Destroy.Melee);
					break;
				default:
					Debug.LogError("Unrecognized state: " + state);
					break;
			}
		}
		#endregion
		
		#region Miscellanious Model Events
		void OnNavigationMeshCalculationState(NavigationMeshModel.CalculationStates calculationState)
		{
			if (calculationState == NavigationMeshModel.CalculationStates.Completed) Model.RecalculateEntrances();
		}

		void OnLightSensitiveLightLevel(float lightLevel) => Model.RecalculateEntrances();

		protected virtual void OnObligationDestroyMelee(Obligation obligation, IModel source) { }
		#endregion
		
		#region Utility
		protected override bool QueueNavigationCalculation => 0 < Model.Clearable.MeleeRangeBonus.Value;
		#endregion
	}
}