using Lunra.WildVacuum.Ai;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using Lunra.WildVacuum.Views;
using UnityEngine;

namespace Lunra.WildVacuum.Presenters
{
	public class DwellerPresenter : AgentPresenter<DwellerView, DwellerModel, DwellerStateMachine>
	{
		public DwellerPresenter(GameModel game, DwellerModel agent) : base(game, agent) { }

		protected override void OnBind()
		{
			
			
			base.OnBind();
		}

		protected override void OnUnBind()
		{
			base.OnUnBind();
		}
	}
}