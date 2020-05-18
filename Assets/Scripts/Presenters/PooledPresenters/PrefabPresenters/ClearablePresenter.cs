using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
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
			Model.MeleeRangeBonus.Value = View.MeleeRangeBonus;
			
			Model.SelectionState.Changed += OnClearableSelectionState;
			Model.Health.Changed += OnClearableHealth;
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Model.SelectionState.Changed -= OnClearableSelectionState;
			Model.Health.Changed -= OnClearableHealth;
			
			base.UnBind();
		}

		protected override void OnViewPrepare()
		{
			View.Shown += () => OnClearableSelectionState(Model.SelectionState.Value);
		}

		#region ClearableModel Events
		void OnClearableSelectionState(SelectionStates selectionState)
		{
			if (View.NotVisible) return;
			
			switch (selectionState)
			{
				case SelectionStates.Deselected:
					if (!Model.IsMarkedForClearance.Value) View.Deselect();
					break;
				case SelectionStates.Highlighted: View.Highlight(); break;
				case SelectionStates.Selected:
					View.Select();
					Model.ClearancePriority.Value = 0;
					break;
			}
		}

		void OnClearableHealth(float health)
		{
			if (Mathf.Approximately(0f, health)) Model.PooledState.Value = PooledStates.InActive;
		}
		#endregion
		
		#region Utility
		protected override bool QueueNavigationCalculation => 0 < Model.MeleeRangeBonus.Value;
		#endregion
	}
}