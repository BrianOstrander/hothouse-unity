using System;
using System.Linq;
using Lunra.Core;
using Lunra.NumberDemon;
using Lunra.StyxMvp;
using Lunra.StyxMvp.Presenters;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Lunra.WildVacuum.Presenters
{
	public class FloraPresenter : Presenter<FloraView>
	{
		GameModel game;
		FloraModel flora;

		public FloraPresenter(
			GameModel game,
			FloraModel flora
		)
		{
			this.game = game;
			this.flora = flora;

			flora.HasPresenter.Value = true;

			if (string.IsNullOrEmpty(flora.Id.Value)) flora.Id.Value = Guid.NewGuid().ToString();
			
			game.SimulationUpdate += OnGameSimulationUpdate;

			flora.State.Changed += OnFloraState;
			flora.IsReproducing.Changed += OnFloraIsReproducing;
			flora.SelectionState.Changed += OnFloraSelectionState;
			flora.Health.Changed += OnFloraHealth;
			
			if (game.IsSimulationInitialized) OnInitialized();
			else game.SimulationInitialize += OnInitialized;
		}

		protected override void OnUnBind()
		{
			game.SimulationInitialize -= OnInitialized;
			game.SimulationUpdate -= OnGameSimulationUpdate;

			flora.State.Changed -= OnFloraState;
			flora.IsReproducing.Changed -= OnFloraIsReproducing;
			flora.SelectionState.Changed -= OnFloraSelectionState;
			flora.Health.Changed -= OnFloraHealth;
		}
		
		void Show()
		{
			if (View.Visible) return;
			
			View.Reset();
			
			View.Age = flora.Age.Value.Normalized;
			View.IsReproducing = flora.IsReproducing.Value;
			
			ShowView(instant: true);

			View.RootTransform.position = flora.Position.Value;
			View.RootTransform.rotation = flora.Rotation.Value;

			OnFloraSelectionState(flora.SelectionState.Value);
			
			if (Mathf.Approximately(0f, flora.Age.Value.Current)) game.FloraEffects.SpawnQueue.Enqueue(new FloraEffectsModel.Request(flora.Position.Value));
		}

		void Close()
		{
			if (View.NotVisible) return;
			
			CloseView(true);
		}

		void OnReproduce()
		{
			var nearbyFlora = game.Flora.GetActive().Where(
				f =>
				{
					if (f.RoomId.Value != flora.RoomId.Value) return false;
					return Vector3.Distance(f.Position.Value, flora.Position.Value) < (f.ReproductionRadius.Value.Maximum + flora.ReproductionRadius.Value.Maximum);
				}
			);

			var randomPosition = flora.Position.Value + (Random.insideUnitSphere.NewY(0f).normalized * flora.ReproductionRadius.Value.Evaluate(DemonUtility.NextFloat));

			var reproductionFailed = true;
			
			if (NavMesh.SamplePosition(randomPosition, out var hit, flora.ReproductionRadius.Value.Delta, NavMesh.AllAreas))
			{
				var distance = Vector3.Distance(flora.Position.Value, hit.position);
				if (flora.ReproductionRadius.Value.Minimum < distance && distance < flora.ReproductionRadius.Value.Maximum)
				{
					if (nearbyFlora.None(f => Vector3.Distance(f.Position.Value, hit.position) < f.ReproductionRadius.Value.Minimum))
					{
						if (game.Dwellers.GetActive().None(d => Vector3.Distance(d.Position.Value, hit.position) < flora.ReproductionRadius.Value.Minimum))
						{
							reproductionFailed = false;

							game.Flora.Activate(
								newFlora =>
								{
									newFlora.RoomId.Value = flora.RoomId.Value;
									newFlora.Position.Value = hit.position;
									newFlora.Rotation.Value = Quaternion.AngleAxis(DemonUtility.GetNextFloat(0f, 360f), Vector3.up);
									newFlora.Age.Value = FloraModel.Interval.Create(flora.Age.Value.Maximum);
									newFlora.ReproductionElapsed.Value = FloraModel.Interval.Create(flora.ReproductionElapsed.Value.Maximum);
									newFlora.ReproductionRadius.Value = flora.ReproductionRadius.Value;
									newFlora.ReproductionFailures.Value = 0;
									newFlora.ReproductionFailureLimit.Value = flora.ReproductionFailureLimit.Value;
									newFlora.HealthMaximum.Value = flora.HealthMaximum.Value;
									newFlora.Health.Value = flora.HealthMaximum.Value;
									newFlora.MarkedForClearing.Value = false;
									newFlora.ItemDrops.Value = flora.ItemDrops.Value;
									
									if (game.Selection.Current.Value.State == SelectionModel.States.Highlighting && game.Selection.Current.Value.Contains(newFlora.Position.Value))
									{
										newFlora.SelectionState.Value = SelectionStates.Highlighted;
									}
									else newFlora.SelectionState.Value = SelectionStates.Deselected;

									newFlora.State.Value = FloraModel.States.Visible;

									if (!newFlora.HasPresenter.Value) new FloraPresenter(game, newFlora);
								}
							);
						}
					}
				}
			}

			if (reproductionFailed) flora.ReproductionFailures.Value++;
			
			flora.ReproductionElapsed.Value = FloraModel.Interval.Create(flora.ReproductionElapsed.Value.Maximum);
		}
		
		#region Events
		void OnInitialized()
		{
			OnFloraState(flora.State.Value);
		}
		#endregion

		#region Heartbeat Events
		
		#endregion
		
		#region GameModel Events
		void OnGameSimulationUpdate(float delta)
		{
			switch (flora.State.Value)
			{
				case FloraModel.States.Pooled:
					return;
			}

			if (!flora.Age.Value.IsDone)
			{
				flora.Age.Value = flora.Age.Value.Update(delta);

				if (View.Visible) View.Age = flora.Age.Value.Normalized;
				
				return;
			}
			
			if (flora.ReproductionFailures.Value == flora.ReproductionFailureLimit.Value) return;

			if (!flora.ReproductionElapsed.Value.IsDone)
			{
				flora.ReproductionElapsed.Value = flora.ReproductionElapsed.Value.Update(delta);
				return;
			}

			if (250 < game.Flora.GetActive().Length) return;
			
			OnReproduce();
		}
		#endregion

		#region FloraModel Events
		void OnFloraState(FloraModel.States state)
		{
			switch (state)
			{
				case FloraModel.States.Pooled:
				case FloraModel.States.NotVisible:
					Close();
					break;
				case FloraModel.States.Visible:
					Show();
					break;
				default:
					Debug.LogError("Unrecognized state: " + state);
					break;
			}
		}

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
					if (!flora.MarkedForClearing.Value) View.Deselect();
					break;
				case SelectionStates.Highlighted: View.Highlight(); break;
				case SelectionStates.Selected:
					View.Select();
					flora.MarkedForClearing.Value = true;
					break;
			}
		}

		void OnFloraHealth(float health)
		{
			if (!Mathf.Approximately(0f, health))
			{
				if (View.Visible) game.FloraEffects.HurtQueue.Enqueue(new FloraEffectsModel.Request(flora.Position.Value));
				return;
			}
			
			if (View.Visible) game.FloraEffects.DeathQueue.Enqueue(new FloraEffectsModel.Request(flora.Position.Value));
			
			flora.State.Value = FloraModel.States.Pooled;
			game.Flora.InActivate(flora);
		}
		#endregion
	}
}