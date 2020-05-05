using System;
using System.Linq;
using Lunra.Core;
using Lunra.NumberDemon;
using Lunra.StyxMvp.Presenters;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Lunra.WildVacuum.Presenters
{
	public class FloraPresenter : PooledPresenter<FloraView, FloraModel>
	{
		public FloraPresenter(GameModel game, FloraModel model) : base(game, model) { }

		protected override void OnBind()
		{
			base.OnBind();
			
			Game.SimulationUpdate += OnGameSimulationUpdate;

			Model.IsReproducing.Changed += OnFloraIsReproducing;
			Model.SelectionState.Changed += OnFloraSelectionState;
			Model.Health.Changed += OnFloraHealth;
		}

		protected override void OnUnBind()
		{
			base.OnUnBind();
			
			Game.SimulationUpdate -= OnGameSimulationUpdate;

			Model.IsReproducing.Changed -= OnFloraIsReproducing;
			Model.SelectionState.Changed -= OnFloraSelectionState;
			Model.Health.Changed -= OnFloraHealth;
		}

		protected override void OnShow()
		{
			View.Age = Model.Age.Value.Normalized;
			View.IsReproducing = Model.IsReproducing.Value;
			
			View.Shown += () => OnFloraSelectionState(Model.SelectionState.Value);
			
			if (Mathf.Approximately(0f, Model.Age.Value.Current)) Game.FloraEffects.SpawnQueue.Enqueue(new FloraEffectsModel.Request(Model.Position.Value));
		}

		#region GameModel Events
		void OnGameSimulationUpdate(float delta)
		{
			switch (Model.PooledState.Value)
			{
				case PooledStates.Pooled:
					return;
			}

			if (!Model.Age.Value.IsDone)
			{
				Model.Age.Value = Model.Age.Value.Update(delta);

				if (View.Visible) View.Age = Model.Age.Value.Normalized;
				
				return;
			}
			
			if (Model.ReproductionFailures.Value == Model.ReproductionFailureLimit.Value) return;

			if (!Model.ReproductionElapsed.Value.IsDone)
			{
				Model.ReproductionElapsed.Value = Model.ReproductionElapsed.Value.Update(delta);
				return;
			}

			if (250 < Game.Flora.GetActive().Length) return;
			
			TryReproducing();
		}
		#endregion

		#region FloraModel Events
		void OnFloraIsReproducing(bool isReproducing)
		{
			if (View.NotVisible) return;
			View.IsReproducing = isReproducing;
		}
		
		void OnFloraSelectionState(SelectionStates selectionState)
		{
			if (View.NotVisible) return;
			
			switch (selectionState)
			{
				case SelectionStates.Deselected:
					if (!Model.MarkedForClearing.Value) View.Deselect();
					break;
				case SelectionStates.Highlighted: View.Highlight(); break;
				case SelectionStates.Selected:
					View.Select();
					Model.MarkedForClearing.Value = true;
					break;
			}
		}

		void OnFloraHealth(float health)
		{
			if (!Mathf.Approximately(0f, health))
			{
				if (View.Visible) Game.FloraEffects.HurtQueue.Enqueue(new FloraEffectsModel.Request(Model.Position.Value));
				return;
			}
			
			if (View.Visible) Game.FloraEffects.DeathQueue.Enqueue(new FloraEffectsModel.Request(Model.Position.Value));
			
			Model.PooledState.Value = PooledStates.Pooled;
			Game.Flora.InActivate(Model);
		}
		#endregion
		
		#region Utility
		void TryReproducing()
		{
			var nearbyFlora = Game.Flora.GetActive().Where(
				f =>
				{
					if (f.RoomId.Value != Model.RoomId.Value) return false;
					return Vector3.Distance(f.Position.Value, Model.Position.Value) < (f.ReproductionRadius.Value.Maximum + Model.ReproductionRadius.Value.Maximum);
				}
			);

			var randomPosition = Model.Position.Value + (Random.insideUnitSphere.NewY(0f).normalized * Model.ReproductionRadius.Value.Evaluate(DemonUtility.NextFloat));

			var reproductionFailed = true;
			
			if (NavMesh.SamplePosition(randomPosition, out var hit, Model.ReproductionRadius.Value.Delta, NavMesh.AllAreas))
			{
				var distance = Vector3.Distance(Model.Position.Value, hit.position);
				if (Model.ReproductionRadius.Value.Minimum < distance && distance < Model.ReproductionRadius.Value.Maximum)
				{
					if (nearbyFlora.None(f => Vector3.Distance(f.Position.Value, hit.position) < f.ReproductionRadius.Value.Minimum))
					{
						if (Game.Dwellers.GetActive().None(d => Vector3.Distance(d.Position.Value, hit.position) < Model.ReproductionRadius.Value.Minimum))
						{
							reproductionFailed = false;

							Game.Flora.Activate(
								newFlora =>
								{
									newFlora.RoomId.Value = Model.RoomId.Value;
									newFlora.Position.Value = hit.position;
									newFlora.Rotation.Value = Quaternion.AngleAxis(DemonUtility.GetNextFloat(0f, 360f), Vector3.up);
									newFlora.Age.Value = Interval.WithMaximum(Model.Age.Value.Maximum);
									newFlora.ReproductionElapsed.Value = Interval.WithMaximum(Model.ReproductionElapsed.Value.Maximum);
									newFlora.ReproductionRadius.Value = Model.ReproductionRadius.Value;
									newFlora.ReproductionFailures.Value = 0;
									newFlora.ReproductionFailureLimit.Value = Model.ReproductionFailureLimit.Value;
									newFlora.HealthMaximum.Value = Model.HealthMaximum.Value;
									newFlora.Health.Value = Model.HealthMaximum.Value;
									newFlora.MarkedForClearing.Value = false;
									newFlora.ItemDrops.Value = Model.ItemDrops.Value;
									
									if (Game.Selection.Current.Value.State == SelectionModel.States.Highlighting && Game.Selection.Current.Value.Contains(newFlora.Position.Value))
									{
										newFlora.SelectionState.Value = SelectionStates.Highlighted;
									}
									else newFlora.SelectionState.Value = SelectionStates.Deselected;

									newFlora.PooledState.Value = PooledStates.Visible;

									if (!newFlora.HasPresenter.Value) new FloraPresenter(Game, newFlora);
								}
							);
						}
					}
				}
			}

			if (reproductionFailed) Model.ReproductionFailures.Value++;
			
			Model.ReproductionElapsed.Value = Interval.WithMaximum(Model.ReproductionElapsed.Value.Maximum);
		}
		#endregion
	}
}