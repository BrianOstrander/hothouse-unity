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

			base.Bind();
		}

		protected override void UnBind()
		{
			Model.DesireUpdated -= OnDwellerDesireUpdated;
			
			Model.Health.Damaged -= OnDwellerHealthDamage;
			
			if (Model.Bed.Value.TryGetInstance<BuildingModel>(Game, out var bed)) bed.Ownership.Remove(Model);
			
			base.UnBind();
		}
		
		#region DwellerModel Events
		void OnDwellerDesireUpdated(Desires desire, bool filled)
		{
			if (View.NotVisible) return;
			
			View.PlayDesire(desire, filled);
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

		public override void OnAgentObligationComplete(Obligation obligation)
		{
			Debug.Log("uhhh: "+obligation);
			if (View.NotVisible) return;
			if (!obligation.Type.Equals(ObligationCategories.Door.Open)) return;

			var nearestDoor = Game.Doors.AllActive
				.Where(m => m.IsOpen.Value)
				.OrderBy(m => Vector3.Distance(m.Transform.Position.Value, Model.Transform.Position.Value))
				.FirstOrDefault();

			if (nearestDoor == null)
			{
				Debug.LogError("Door was opened, but no valid target found");
				return;
			}

			View.LaunchGlowstick((nearestDoor.Transform.Position.Value - Model.Transform.Position.Value).normalized);
		}
	}
}