using System;
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

			Game.Toolbar.ClearanceTask.Changed += OnToolbarClearanceTask;
				
			Model.SelectionState.Changed += OnClearableSelectionState;
			Model.Health.Current.Changed += OnClearableHealthCurrent;
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Game.Toolbar.ClearanceTask.Changed -= OnToolbarClearanceTask;
			
			Model.SelectionState.Changed -= OnClearableSelectionState;
			Model.Health.Current.Changed -= OnClearableHealthCurrent;
			
			base.UnBind();
		}

		protected override void OnViewPrepare()
		{
			View.Shown += () => OnClearableSelectionState(Model.SelectionState.Value);
		}

		#region ClearableModel Events
		void OnClearableSelectionState(SelectionStates selectionState)
		{
			if (IsNotActive) return;
			if (View.NotVisible) return;
			
			switch (selectionState)
			{
				case SelectionStates.NotSelected:
					if (!Model.IsMarkedForClearance.Value) View.Deselect();
					break;
				case SelectionStates.Highlighted: View.Highlight(); break;
				case SelectionStates.Selected:
					View.Select();
					Model.ClearancePriority.Value = 0;
					break;
			}
		}

		void OnClearableHealthCurrent(float health)
		{
			if (IsNotActive) return;
			if (Mathf.Approximately(0f, health)) Model.PooledState.Value = PooledStates.InActive;
		}
		#endregion

		#region ToolbarModel Events
		void OnToolbarClearanceTask(Interaction.GenericVector3 interaction)
		{
			if (IsNotActive) return;
			if (Model.IsMarkedForClearance.Value) return;
			if (interaction.State == Interaction.States.OutOfRange) return;
			
			var radiusContains = interaction.Value.RadiusContains(Model.Transform.Position.Value);
			
			switch (interaction.State)
			{
				case Interaction.States.Idle:
					break;
				case Interaction.States.Begin:
				case Interaction.States.Active:
					Model.SelectionState.Value = radiusContains ? SelectionStates.Highlighted : SelectionStates.NotSelected;
					break;
				case Interaction.States.End:
					Model.SelectionState.Value = radiusContains ? SelectionStates.Selected : SelectionStates.NotSelected;
					break;
				case Interaction.States.Cancel:
					Model.SelectionState.Value = SelectionStates.NotSelected;
					break;
				default:
					Debug.LogError("Unrecognized Interaction.State: "+interaction.State);
					break;
			}
		}
		#endregion
		
		#region Utility
		protected override bool QueueNavigationCalculation => 0 < Model.MeleeRangeBonus.Value;
		#endregion
	}
}