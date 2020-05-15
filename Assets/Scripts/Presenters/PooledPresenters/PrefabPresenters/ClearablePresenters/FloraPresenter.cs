using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.NumberDemon;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Lunra.Hothouse.Presenters
{
	public class FloraPresenter : ClearablePresenter<FloraModel, FloraView>
	{
		public FloraPresenter(GameModel game, FloraModel model) : base(game, model) { }

		protected override void Bind()
		{
			base.Bind();
			
			Game.SimulationUpdate += OnGameSimulationUpdate;

			Model.Health.Changed += OnFloraHealth;
			Model.IsReproducing.Changed += OnFloraIsReproducing;
		}

		protected override void UnBind()
		{
			base.UnBind();
			
			Game.SimulationUpdate -= OnGameSimulationUpdate;

			Model.Health.Changed -= OnFloraHealth;
			Model.IsReproducing.Changed -= OnFloraIsReproducing;
		}

		protected override void OnViewPrepare()
		{
			base.OnViewPrepare();
			
			View.Age = Model.Age.Value.Normalized;
			View.IsReproducing = Model.IsReproducing.Value;
			
			if (Mathf.Approximately(0f, Model.Age.Value.Current)) Game.FloraEffects.SpawnQueue.Enqueue(new FloraEffectsModel.Request(Model.Position.Value));
		}

		#region GameModel Events
		void OnGameSimulationUpdate()
		{
			switch (Model.PooledState.Value)
			{
				case PooledStates.InActive:
					return;
			}

			if (!Model.Age.Value.IsDone)
			{
				Model.Age.Value = Model.Age.Value.Update(Game.SimulationDelta);

				if (View.Visible) View.Age = Model.Age.Value.Normalized;
				
				return;
			}
			
			if (Model.ReproductionFailures.Value == Model.ReproductionFailureLimit.Value) return;

			if (!Model.ReproductionElapsed.Value.IsDone)
			{
				Model.ReproductionElapsed.Value = Model.ReproductionElapsed.Value.Update(Game.SimulationDelta);
				return;
			}
			
			TryReproducing();
		}
		#endregion

		#region FloraModel Events
		void OnFloraIsReproducing(bool isReproducing)
		{
			if (View.NotVisible) return;
			View.IsReproducing = isReproducing;
		}

		void OnFloraHealth(float health)
		{
			if (!Mathf.Approximately(0f, health))
			{
				if (View.Visible) Game.FloraEffects.HurtQueue.Enqueue(new FloraEffectsModel.Request(Model.Position.Value));
				return;
			}
			
			if (View.Visible) Game.FloraEffects.DeathQueue.Enqueue(new FloraEffectsModel.Request(Model.Position.Value));
		}
		#endregion
		
		#region Utility
		void TryReproducing()
		{
			var nearbyFlora = Game.Flora.AllActive.Where(
				f =>
				{
					if (f.RoomId.Value != Model.RoomId.Value) return false;
					return Vector3.Distance(f.Position.Value, Model.Position.Value) < (f.ReproductionRadius.Value.Maximum + Model.ReproductionRadius.Value.Maximum);
				}
			);

			var randomPosition = Model.Position.Value + (Random.insideUnitSphere.NewY(0f).normalized * Model.ReproductionRadius.Value.Evaluate(DemonUtility.NextFloat));

			var increaseReproductionFailures = true;
			
			if (NavMesh.SamplePosition(randomPosition, out var hit, Model.ReproductionRadius.Value.Delta, NavMesh.AllAreas))
			{
				var distance = Vector3.Distance(Model.Position.Value, hit.position);
				if (Model.ReproductionRadius.Value.Minimum < distance && distance < Model.ReproductionRadius.Value.Maximum)
				{
					if (nearbyFlora.None(f => Vector3.Distance(f.Position.Value, hit.position) < f.ReproductionRadius.Value.Minimum))
					{
						if (Game.Dwellers.AllActive.None(d => Vector3.Distance(d.Position.Value, hit.position) < Model.ReproductionRadius.Value.Minimum))
						{
							increaseReproductionFailures = false;

							Game.Flora.Activate(
								Model.ValidPrefabIds.Value.Random(),
								newFlora =>
								{
									newFlora.ValidPrefabIds.Value = Model.ValidPrefabIds.Value;
									newFlora.Species.Value = Model.Species.Value;
									newFlora.RoomId.Value = Model.RoomId.Value;
									newFlora.Position.Value = hit.position;
									newFlora.Rotation.Value = Quaternion.AngleAxis(DemonUtility.GetNextFloat(0f, 360f), Vector3.up);
									newFlora.Age.Value = Interval.WithMaximum(Model.Age.Value.Maximum);
									newFlora.ReproductionElapsed.Value = Interval.WithMaximum(Model.ReproductionElapsed.Value.Maximum);
									newFlora.ReproductionRadius.Value = Model.ReproductionRadius.Value;
									newFlora.ReproductionFailures.Value = 0;
									newFlora.ReproductionFailureLimit.Value = Model.ReproductionFailureLimit.Value;
									newFlora.SpreadDamage.Value = Model.SpreadDamage.Value;
									newFlora.HealthMaximum.Value = Model.HealthMaximum.Value;
									newFlora.Health.Value = Model.HealthMaximum.Value;
									newFlora.ClearancePriority.Value = null;
									newFlora.ItemDrops.Value = Model.ItemDrops.Value;
									
									if (Game.Selection.Current.Value.State == SelectionModel.States.Highlighting && Game.Selection.Current.Value.Contains(newFlora.Position.Value))
									{
										newFlora.SelectionState.Value = SelectionStates.Highlighted;
									}
									else newFlora.SelectionState.Value = SelectionStates.Deselected;
								}
							);
						}
					}
				}
			}

			if (increaseReproductionFailures && 0f < Model.SpreadDamage.Value)
			{
				var nearestFloraOfDifferentSpecies = Game.Flora.AllActive
					.Where(
						f =>
						{
							if (f.Species.Value == Model.Species.Value) return false;
							return Vector3.Distance(f.Position.Value, Model.Position.Value) < Model.ReproductionRadius.Value.Maximum;
						}
					)
					.OrderBy(f => Vector3.Distance(f.Position.Value, Model.Position.Value))
					.FirstOrDefault();
				
				if (nearestFloraOfDifferentSpecies != null)
				{
					nearestFloraOfDifferentSpecies.Health.Value = Mathf.Max(0f, nearestFloraOfDifferentSpecies.Health.Value - Model.SpreadDamage.Value);
					increaseReproductionFailures = false;
				}
			}
			
			if (increaseReproductionFailures) Model.ReproductionFailures.Value++;
			
			Model.ReproductionElapsed.Value = Interval.WithMaximum(Model.ReproductionElapsed.Value.Maximum);
		}
		#endregion
	}
}