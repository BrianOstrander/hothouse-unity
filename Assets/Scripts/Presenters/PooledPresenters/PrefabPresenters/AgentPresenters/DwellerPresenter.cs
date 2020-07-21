using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Ai.Dweller;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class DwellerPresenter : AgentPresenter<DwellerModel, DwellerView, DwellerStateMachine>
	{
		public DwellerPresenter(GameModel game, DwellerModel model) : base(game, model) { }

		protected override void Bind()
		{
			Model.DesireUpdated += OnDwellerDesireUpdated;
			
			Model.Health.Damaged += OnDwellerHealthDamage;
			
			Game.SimulationUpdate += OnGameSimulationUpdate;

			base.Bind();
		}

		protected override void UnBind()
		{
			Model.DesireUpdated -= OnDwellerDesireUpdated;
			
			Model.Health.Damaged -= OnDwellerHealthDamage;
			
			Game.SimulationUpdate -= OnGameSimulationUpdate;
			
			if (Model.Bed.Value.TryGetInstance<BuildingModel>(Game, out var bed)) bed.Ownership.Remove(Model);
			
			base.UnBind();
		}
		
		#region DwellerModel Events
		void OnDwellerDesireUpdated(Motives motive, bool filled)
		{
			if (View.NotVisible) return;
			
			View.PlayDesire(motive, filled);
		}

		void OnDwellerHealthDamage(Damage.Result result)
		{
			if (result.IsTargetDestroyed)
			{
				Game.EventLog.DwellerEntries.Enqueue(
					new EventLogModel.Entry(
						StringExtensions.Wrap(
							Model.Name.Value + " died from " + result.Type,
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
				Game.EventLog.DwellerEntries.Enqueue(
					new EventLogModel.Entry(
						StringExtensions.Wrap(
							Model.Name.Value + " is suffering from " + result.Type,
							"<color=yellow>",
							"</color>"
						),
						Game.SimulationTime.Value,
						Model.GetInstanceId()
					)
				);
			}
		}
		#endregion

		void OnGameSimulationUpdate()
		{
			Model.Goals.Update(Game.SimulationDelta);
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