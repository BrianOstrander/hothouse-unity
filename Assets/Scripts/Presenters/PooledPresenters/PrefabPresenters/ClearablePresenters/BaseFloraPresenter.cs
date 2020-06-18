using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.NumberDemon;
using UnityEngine;
using UnityEngine.AI;

namespace Lunra.Hothouse.Presenters
{
	public class BaseFloraPresenter<V> : ClearablePresenter<FloraModel, V>
		where V : FloraView
	{
		public enum ReproductionEvents
		{
			Unknown = 0,
			Default = 10,
			ReproduceChild = 20,
			ReproduceAdult = 30,
			Custom = 40
		}

		protected virtual ReproductionEvents DefaultReproductionEvent => ReproductionEvents.ReproduceChild; 
		
		Demon generator = new Demon();
		
		public BaseFloraPresenter(GameModel game, FloraModel model) : base(game, model) { }

		protected override void Bind()
		{
			base.Bind();
			
			Game.SimulationUpdate += OnGameSimulationUpdate;

			Model.TriggerReproduction = OnFloraTriggerReproduction;

			Model.Health.Current.Changed += OnFloraHealthCurrent;
			Model.IsReproducing.Changed += OnFloraIsReproducing;
		}

		protected override void UnBind()
		{
			base.UnBind();
			
			Game.SimulationUpdate -= OnGameSimulationUpdate;
			
			Model.TriggerReproduction = null;

			Model.Health.Current.Changed -= OnFloraHealthCurrent;
			Model.IsReproducing.Changed -= OnFloraIsReproducing;
		}

		protected override void OnViewPrepare()
		{
			base.OnViewPrepare();
			
			View.Age = Model.Age.Value.Normalized;
			View.IsReproducing = Model.IsReproducing.Value;
			
			if (Mathf.Approximately(0f, Model.Age.Value.Current)) Game.Effects.Queued.Enqueue(new EffectsModel.Request(Model.Transform.Position.Value, View.SpawnEffectId));
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

		void OnFloraHealthCurrent(float health)
		{
			if (!Mathf.Approximately(0f, health))
			{
				if (View.Visible) Game.Effects.Queued.Enqueue(new EffectsModel.Request(Model.Transform.Position.Value, View.HurtEffectId));
				return;
			}
			
			if (View.Visible) Game.Effects.Queued.Enqueue(new EffectsModel.Request(Model.Transform.Position.Value, View.DeathEffectId));
		}

		bool OnFloraTriggerReproduction(Demon generatorOverride)
		{
			return TryReproducing(
				ReproductionEvents.ReproduceAdult,
				false,
				generatorOverride
			);
		}
		#endregion
		
		#region Utility
		bool TryReproducing(
			ReproductionEvents reproductionEvent = ReproductionEvents.Default,
			bool incrementFailures = true,
			Demon generatorOverride = null
		)
		{
			if (reproductionEvent == ReproductionEvents.Default) reproductionEvent = DefaultReproductionEvent;
			
			var currentGenerator = generatorOverride ?? generator;
								
			var nearbyFlora = Game.Flora.AllActive.Where(
				f =>
				{
					if (f.RoomTransform.Id.Value != Model.RoomTransform.Id.Value)
					{
						if (!Room.AdjacentRoomIds.Value.TryGetValue(f.RoomTransform.Id.Value, out var openTo) || !openTo) return false;
					}
					return Vector3.Distance(f.Transform.Position.Value, Model.Transform.Position.Value) < (f.ReproductionRadius.Value.Maximum + Model.ReproductionRadius.Value.Maximum);
				}
			);

			var randomPosition = Model.Transform.Position.Value + (currentGenerator.NextNormal * Model.ReproductionRadius.Value.Evaluate(currentGenerator.NextFloat));

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
							var hasFloorHit = Physics.Raycast(
								new Ray(hit.position + Vector3.up, Vector3.down),
								out var floorHit,
								3f, // TODO: Don't hardcode this
								LayerMasks.Floor
							);

							if (hasFloorHit)
							{
								var roomView = floorHit.transform.GetAncestor<IRoomIdView>();
								if (roomView != null)
								{
									increaseReproductionFailures = false;

									switch (reproductionEvent)
									{
										case ReproductionEvents.ReproduceChild:
											Game.Flora.ActivateChild(
												Model.Species.Value,
												roomView.RoomId,
												hit.position
											);
											break;
										case ReproductionEvents.ReproduceAdult:
											Game.Flora.ActivateAdult(
												Model.Species.Value,
												roomView.RoomId,
												hit.position
											);
											break;
										case ReproductionEvents.Custom:
											OnReproductionCustom(
												roomView.RoomId,
												hit.position
											);
											break;
										default:
											Debug.LogError("Unrecognized ReproductionEvent: "+reproductionEvent);
											break;
									}
								}
							}
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
					Damage.ApplyGeneric(
						Model.SpreadDamage.Value,
						Model,
						nearestFloraOfDifferentSpecies
					);
					increaseReproductionFailures = false;
				}
				else if (Model.AttacksBuildings.Value)
				{
					// TODO: Make a ray trace to see what's actually the building blocking us...
					var room = Game.Rooms.FirstActive(Model.RoomTransform.Id.Value);
					var nearestBuilding = Game.Buildings.AllActive
						.Where(m => m.RoomTransform.Id.Value == Model.RoomTransform.Id.Value || (room.AdjacentRoomIds.Value.TryGetValue(m.RoomTransform.Id.Value, out var isOpen) && isOpen))
						.FirstOrDefault(m => m.Boundary.Contains(randomPosition));

					if (nearestBuilding != null)
					{
						Damage.ApplyGeneric(
							Model.SpreadDamage.Value,
							Model,
							nearestBuilding
						);
						increaseReproductionFailures = false;
					}
				}
			}
			
			if (increaseReproductionFailures && incrementFailures) Model.ReproductionFailures.Value++;
			
			Model.ReproductionElapsed.Value = Interval.WithMaximum(Model.ReproductionElapsed.Value.Maximum);

			return !increaseReproductionFailures;
		}

		protected virtual void OnReproductionCustom(string roomId, Vector3 position) => Debug.LogWarning("This presenter has not specified a custom reproduction event");
		#endregion
	}
}