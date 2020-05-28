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
			
			if (Mathf.Approximately(0f, Model.Age.Value.Current)) Game.FloraEffects.SpawnQueue.Enqueue(new FloraEffectsModel.Request(Model.Transform.Position.Value));
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
				if (View.Visible) Game.FloraEffects.HurtQueue.Enqueue(new FloraEffectsModel.Request(Model.Transform.Position.Value));
				return;
			}
			
			if (View.Visible) Game.FloraEffects.DeathQueue.Enqueue(new FloraEffectsModel.Request(Model.Transform.Position.Value));
		}
		#endregion
		
		#region Utility
		void TryReproducing()
		{
			var nearbyFlora = Game.Flora.AllActive.Where(
				f =>
				{
					if (f.RoomId.Value != Model.RoomId.Value) return false;
					return Vector3.Distance(f.Transform.Position.Value, Model.Transform.Position.Value) < (f.ReproductionRadius.Value.Maximum + Model.ReproductionRadius.Value.Maximum);
				}
			);

			var randomPosition = Model.Transform.Position.Value + (Random.insideUnitSphere.NewY(0f).normalized * Model.ReproductionRadius.Value.Evaluate(DemonUtility.NextFloat));

			var increaseReproductionFailures = true;
			
			if (NavMesh.SamplePosition(randomPosition, out var hit, Model.ReproductionRadius.Value.Delta, NavMesh.AllAreas))
			{
				var distance = Vector3.Distance(Model.Transform.Position.Value, hit.position);
				if (Model.ReproductionRadius.Value.Minimum < distance && distance < Model.ReproductionRadius.Value.Maximum)
				{
					if (nearbyFlora.None(f => Vector3.Distance(f.Transform.Position.Value, hit.position) < f.ReproductionRadius.Value.Minimum))
					{
						if (Game.Dwellers.AllActive.None(d => Vector3.Distance(d.Transform.Position.Value, hit.position) < Model.ReproductionRadius.Value.Minimum))
						{
							increaseReproductionFailures = false;

							Game.Flora.ActivateChild(
								Model.Species.Value,
								Model.RoomId.Value,
								hit.position
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
							return Vector3.Distance(f.Transform.Position.Value, Model.Transform.Position.Value) < Model.ReproductionRadius.Value.Maximum;
						}
					)
					.OrderBy(f => Vector3.Distance(f.Transform.Position.Value, Model.Transform.Position.Value))
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