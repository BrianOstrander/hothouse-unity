using Lunra.WildVacuum.Ai;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Models.AgentModels;
using Lunra.WildVacuum.Views;
using UnityEngine;

namespace Lunra.WildVacuum.Presenters
{
	public class DwellerPresenter : AgentPresenter<DwellerModel, DwellerView, DwellerStateMachine>
	{
		public DwellerPresenter(GameModel game, DwellerModel model) : base(game, model) { }

		protected override void OnBind()
		{

			Model.Inventory.Changed += inventory => Debug.Log("Inventory:\n" + inventory);
			
			base.OnBind();
		}

		protected override void OnUnBind()
		{
			base.OnUnBind();
		}
	}
}