using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Ai.Dweller;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class DwellerPresenter : AgentPresenter<DwellerModel, DwellerView, DwellerStateMachine>
	{
		public DwellerPresenter(GameModel game, DwellerModel model) : base(game, model) { }

		protected override void Bind()
		{
			Model.Goals.Bind();
			
			Model.Health.Damaged += OnDwellerHealthDamage;
			
			Game.SimulationUpdate += OnGameSimulationUpdate;

			base.Bind();
		}

		protected override void UnBind()
		{
			Model.Goals.UnBind();
			
			Model.Health.Damaged -= OnDwellerHealthDamage;
			
			Game.SimulationUpdate -= OnGameSimulationUpdate;
			
			if (Model.Bed.Value.TryGetInstance<BuildingModel>(Game, out var bed)) bed.Ownership.Remove(Model);
			
			base.UnBind();
		}
		
		#region DwellerModel Events
		void OnDwellerHealthDamage(Damage.Result result)
		{
			var affliction = string.Empty;
			
			switch (result.Type)
			{
				case Damage.Types.Generic:
					if (result.IsSelfInflicted) affliction += "self";
					else if (result.Source.TryGetInstance<IModel>(Game, out var source)) affliction += source.ShortId;
					else affliction += "unknown";
					break;
				case Damage.Types.GoalHurt:
					var goalsAtMaximum = Model.Goals.Caches
						.Where(c => 0f < c.SimulatedTimeAtMaximum)
						.OrderByDescending(c => c.SimulatedTimeAtMaximum)
						.ToArray();

					for (var i = 0; i < goalsAtMaximum.Length; i++)
					{
						affliction += goalsAtMaximum[i].Motive;

						if (1 < goalsAtMaximum.Length)
						{
							if (i < (goalsAtMaximum.Length - 1)) affliction += ", ";
						}
					}
					break;
				default:
					Debug.LogError("Unrecognized Damage Type: "+result.Type);
					affliction += "UNKNOWN - " + result.Type;
					break;
			}
		
			if (result.IsTargetDestroyed)
			{
				Game.EventLog.Dwellers.Push(
					new EventLogModel.Entry(
						(Model.Name.Value + " died from " + affliction).Wrap(
							"<color=red>",
							"</color>"
						),
						Game.SimulationTime.Value,
						Model.GetInstanceId()
					)
				);
			}
			else
			{
				Game.EventLog.Dwellers.Push(
					new EventLogModel.Entry(
						(Model.Name.Value + " is suffering from " + affliction).Wrap(
							"<color=yellow>",
							"</color>"
						),
						Game.SimulationTime.Value,
						Model.GetInstanceId()
					)
				);
				if (result.Type != Damage.Types.GoalHurt)
				{
					Model.Goals.Apply(
						(Motives.Heal, result.AmountApplied / Model.Health.Maximum.Value)
					);	
				}
			}
		}
		#endregion

		void OnGameSimulationUpdate()
		{
			var newHealth = 1f - Model.Goals[Motives.Heal].Insistence;

			var healthUpdateDelta = (newHealth - Model.Health.Normalized) * Model.Health.Maximum.Value;
			if (!Mathf.Approximately(0f, healthUpdateDelta))
			{
				if (healthUpdateDelta < 0f)
				{
					Damage.Apply(
						Damage.Types.GoalHurt,
						Mathf.Abs(healthUpdateDelta),
						Model
					);
				}
				else Model.Health.Heal(healthUpdateDelta);
			}
		}
		
		public override void OnAgentObligationComplete(Obligation obligation)
		{
			if (View.NotVisible) return;

			if (obligation.Type.Equals(ObligationCategories.Door.Open)) OnAgentObligationOpenDoor();
		}

		void OnAgentObligationOpenDoor()
		{
			try
			{
				var nearestDoorEntrance = Game.Doors.AllActive
					.Where(m => m.IsOpen.Value)
					.Where(m => m.IsConnnecting(Model.RoomTransform.Id.Value))
					.SelectMany(m => m.Enterable.Entrances.Value)
					.OrderBy(m => Vector3.Distance(m.Position, Model.Transform.Position.Value))
					.First();
				
				View.LaunchGlowstick(-nearestDoorEntrance.Forward);
			}
			catch (Exception e) { Debug.LogException(e); }
			

			// if (nearestDoor == null)
			// {
			// 	Debug.LogError("Door was opened, but no valid target found");
			// 	return;
			// }

			// var angleToDoorOrigin = (nearestDoor.Transform.Position.Value - Model.Transform.Position.Value).normalized;
			// View.LaunchGlowstick();
		}
	}
}