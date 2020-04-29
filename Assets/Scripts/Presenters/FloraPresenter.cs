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
			
			game.SimulationUpdate += OnGameSimulationUpdate;
			game.LastNavigationCalculation.Changed += OnGameLastNavigationCalculation;
			
			flora.IsEnabled.Changed += OnFloraIsEnabled;
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
			game.LastNavigationCalculation.Changed -= OnGameLastNavigationCalculation;

			flora.IsEnabled.Changed -= OnFloraIsEnabled;
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
		}

		void Close()
		{
			if (View.NotVisible) return;
			
			CloseView(true);
		}

		void OnReproduce()
		{
			var nearbyFlora = game.Flora.Value.Where(
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
						var newFlora = new FloraModel();

						newFlora.RoomId.Value = flora.RoomId.Value;
						newFlora.IsEnabled.Value = true;
						newFlora.Position.Value = hit.position;
						newFlora.Rotation.Value = Quaternion.AngleAxis(DemonUtility.GetNextFloat(0f, 360f), Vector3.up);
						newFlora.Age.Value = FloraModel.Interval.Create(flora.Age.Value.Maximum);
						newFlora.ReproductionElapsed.Value = FloraModel.Interval.Create(flora.ReproductionElapsed.Value.Maximum);
						newFlora.ReproductionRadius.Value = flora.ReproductionRadius.Value;
						newFlora.ReproductionFailureLimit.Value = flora.ReproductionFailureLimit.Value;
						newFlora.HealthMaximum.Value = flora.HealthMaximum.Value;
						newFlora.Health.Value = flora.HealthMaximum.Value;

						if (game.Selection.Current.Value.State == SelectionModel.States.Highlighting && game.Selection.Current.Value.Contains(newFlora.Position.Value))
						{
							newFlora.SelectionState.Value = SelectionStates.Highlighted;
						}
						
						game.Flora.Value = game.Flora.Value.Append(newFlora).ToArray();

						reproductionFailed = false;

						// Debug.DrawLine(hit.position, hit.position + (Vector3.up * 2f), Color.green, 0.5f);
					}
				}
			}

			if (reproductionFailed) flora.ReproductionFailures.Value++;
			
			flora.ReproductionElapsed.Value = FloraModel.Interval.Create(flora.ReproductionElapsed.Value.Maximum);
		}
		
		#region Events
		void OnInitialized()
		{
			OnFloraIsEnabled(flora.IsEnabled.Value);
			
			if (View.Visible && Mathf.Approximately(0f, flora.Age.Value.Current)) game.FloraEffects.Spawn(new FloraEffectsModel.Request(flora.Position.Value));
		}
		#endregion
		
		#region GameModel Events
		void OnGameSimulationUpdate(float delta)
		{
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

			if (250 < game.Flora.Value.Length)
			{
				flora.ReproductionFailures.Value = flora.ReproductionFailureLimit.Value;
				return;
			}
			
			OnReproduce();
		}

		void OnGameLastNavigationCalculation(DateTime dateTime)
		{
			var found = NavMesh.SamplePosition(flora.Position.Value, out var hit, flora.ReproductionRadius.Value.Maximum, NavMesh.AllAreas);

			if (found) flora.NavigationPoint.Value = new NavigationProximity(hit.position, Vector3.Distance(hit.position, flora.Position.Value), NavigationProximity.AccessStates.Accessible);
			else flora.NavigationPoint.Value = new NavigationProximity(flora.Position.Value, float.MaxValue, NavigationProximity.AccessStates.NotAccessible);
			
			if (found) Debug.DrawLine(flora.NavigationPoint.Value.Position, flora.NavigationPoint.Value.Position + (Vector3.up * 1f), Color.green, 1f);
		}
		#endregion

		#region FloraModel Events
		void OnFloraIsEnabled(bool enabled)
		{
			if (enabled) Show();
			else Close();
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
				case SelectionStates.Deselected: View.Deselect(); break;
				case SelectionStates.Highlighted: View.Highlight(); break;
				case SelectionStates.Selected:
					View.Select();
					if (flora.NavigationPoint.Value.Access == NavigationProximity.AccessStates.Accessible)
					{
						Debug.DrawLine(flora.Position.Value, flora.NavigationPoint.Value.Position, Color.green, 15f);
						Debug.DrawLine(flora.NavigationPoint.Value.Position, flora.NavigationPoint.Value.Position + (Vector3.up * 1f), Color.green, 15f);
					}
					break;
			}
		}

		void OnFloraHealth(float health)
		{
			if (View.NotVisible) return;

			if (!Mathf.Approximately(0f, health)) return;
			
			game.FloraEffects.Chop(new FloraEffectsModel.Request(flora.Position.Value));
			
		}
		#endregion
	}
}