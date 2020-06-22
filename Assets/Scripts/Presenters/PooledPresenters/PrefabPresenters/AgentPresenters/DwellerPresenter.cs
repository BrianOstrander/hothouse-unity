using Lunra.Core;
using Lunra.Hothouse.Ai.Dweller;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;

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
	}
}