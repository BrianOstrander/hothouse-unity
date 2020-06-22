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
			Model.Clearable.MeleeRangeBonus.Value = View.MeleeRangeBonus;

			Game.Toolbar.ClearanceTask.Changed += OnToolbarClearanceTask;
	
			Model.Clearable.SelectionState.Changed += OnClearableSelectionState;
			Model.Health.Current.Changed += OnClearableHealthCurrent;
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Game.Toolbar.ClearanceTask.Changed -= OnToolbarClearanceTask;
			
			Model.Clearable.SelectionState.Changed -= OnClearableSelectionState;
			Model.Health.Current.Changed -= OnClearableHealthCurrent;
			
			base.UnBind();
		}

		protected override void OnViewPrepare()
		{
			base.OnViewPrepare();
			
			View.Shown += () => OnClearableSelectionState(Model.Clearable.SelectionState.Value);
		}

		#region ClearableModel Events
		void OnClearableSelectionState(SelectionStates selectionState)
		{
			if (View.NotVisible) return;
			
			switch (selectionState)
			{
				case SelectionStates.NotSelected:
					if (!Model.Clearable.IsMarkedForClearance.Value) View.Deselect();
					break;
				case SelectionStates.Highlighted: View.Highlight(); break;
				case SelectionStates.Selected:
					View.Select();
					Model.Clearable.ClearancePriority.Value = 0;
					break;
			}
		}

		void OnClearableHealthCurrent(float health)
		{
			if (Mathf.Approximately(0f, health)) Model.PooledState.Value = PooledStates.InActive;
		}
		#endregion
		
		#region PooledModel Events
		protected override bool CanShow() => Room.IsRevealed.Value;
		#endregion

		#region ToolbarModel Events
		void OnToolbarClearanceTask(Interaction.RoomVector3 interaction)
		{
			if (Model.Clearable.IsMarkedForClearance.Value) return;
			if (interaction.State == Interaction.States.OutOfRange) return;
			
			var radiusContains = interaction.Value.RadiusContains(Model.Transform.Position.Value);
			
			switch (interaction.State)
			{
				case Interaction.States.Idle:
					break;
				case Interaction.States.Begin:
				case Interaction.States.Active:
					Model.Clearable.SelectionState.Value = radiusContains ? SelectionStates.Highlighted : SelectionStates.NotSelected;
					break;
				case Interaction.States.End:
					Model.Clearable.SelectionState.Value = radiusContains ? SelectionStates.Selected : SelectionStates.NotSelected;
					break;
				case Interaction.States.Cancel:
					Model.Clearable.SelectionState.Value = SelectionStates.NotSelected;
					break;
				default:
					Debug.LogError("Unrecognized Interaction.State: "+interaction.State);
					break;
			}
		}
		#endregion
		
		#region Utility
		protected override bool QueueNavigationCalculation => 0 < Model.Clearable.MeleeRangeBonus.Value;
		#endregion
	}
}