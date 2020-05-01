﻿using System;
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

			App.Heartbeat.DrawGizmos += OnHeartbeatDrawGizmos;
			
			flora.HasPresenter.Value = true;

			if (string.IsNullOrEmpty(flora.Id.Value)) flora.Id.Value = Guid.NewGuid().ToString();
			
			game.SimulationUpdate += OnGameSimulationUpdate;
			game.LastNavigationCalculation.Changed += OnGameLastNavigationCalculation;
			
			flora.State.Changed += OnFloraState;
			flora.IsReproducing.Changed += OnFloraIsReproducing;
			flora.SelectionState.Changed += OnFloraSelectionState;
			flora.Health.Changed += OnFloraHealth;
			
			if (game.IsSimulationInitialized) OnInitialized();
			else game.SimulationInitialize += OnInitialized;
		}

		protected override void OnUnBind()
		{
			App.Heartbeat.DrawGizmos -= OnHeartbeatDrawGizmos;
			
			game.SimulationInitialize -= OnInitialized;
			game.SimulationUpdate -= OnGameSimulationUpdate;
			game.LastNavigationCalculation.Changed -= OnGameLastNavigationCalculation;

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

								if (game.Selection.Current.Value.State == SelectionModel.States.Highlighting && game.Selection.Current.Value.Contains(newFlora.Position.Value))
								{
									newFlora.SelectionState.Value = SelectionStates.Highlighted;
								}
								else newFlora.SelectionState.Value = SelectionStates.Deselected;

								newFlora.State.Value = FloraModel.States.Visible;

								if (!newFlora.HasPresenter.Value) new FloraPresenter(game, newFlora);
								
								game.LastNavigationCalculation.Value = DateTime.Now;
							}
						);

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
			OnFloraState(flora.State.Value);
		}
		#endregion

		#region Heartbeat Events
		void OnHeartbeatDrawGizmos(Action cleanup)
		{
			switch (flora.State.Value)
			{
				case FloraModel.States.Pooled:
					return;
			}
			
			Gizmos.color = flora.NavigationPoint.Value.Access == NavigationProximity.AccessStates.Accessible ? Color.green : Color.red;

			Gizmos.DrawWireCube(flora.Position.Value, Vector3.one);
			
			cleanup();
		}
		#endregion
		
		#region GameModel Events
		void OnGameSimulationUpdate(float delta)
		{
			switch (flora.State.Value)
			{
				case FloraModel.States.Pooled:
					return;
			}

			if (flora.NavigationPoint.Value.Access == NavigationProximity.AccessStates.Unknown)
			{
				OnGameLastNavigationCalculation(game.LastNavigationCalculation.Value);
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

		void OnGameLastNavigationCalculation(DateTime dateTime)
		{
			switch (flora.State.Value)
			{
				case FloraModel.States.Pooled:
					return;
			}
			
			var found = NavMesh.SamplePosition(flora.Position.Value, out var hit, flora.ReproductionRadius.Value.Maximum, NavMesh.AllAreas);

			if (found) flora.NavigationPoint.Value = new NavigationProximity(hit.position, Vector3.Distance(hit.position, flora.Position.Value), NavigationProximity.AccessStates.Accessible);
			else flora.NavigationPoint.Value = new NavigationProximity(flora.Position.Value, float.MaxValue, NavigationProximity.AccessStates.NotAccessible);
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
				case SelectionStates.Deselected: View.Deselect(); break;
				case SelectionStates.Highlighted: View.Highlight(); break;
				case SelectionStates.Selected:
					View.Select();
					if (flora.NavigationPoint.Value.Access == NavigationProximity.AccessStates.Accessible)
					{
						// Debug.DrawLine(flora.Position.Value, flora.NavigationPoint.Value.Position, Color.green, 15f);
						// Debug.DrawLine(flora.NavigationPoint.Value.Position, flora.NavigationPoint.Value.Position + (Vector3.up * 1f), Color.green, 15f);
					}

					flora.Health.Value = 0f;
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