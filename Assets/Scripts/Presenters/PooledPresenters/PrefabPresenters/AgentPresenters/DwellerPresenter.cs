using Lunra.Hothouse.Ai;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Models.AgentModels;
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
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Model.DesireUpdated -= OnDwellerDesireUpdated;
			
			base.UnBind();
		}
		
		#region DwellerModel Events
		void OnDwellerDesireUpdated(Desires desire, bool filled)
		{
			if (View.NotVisible) return;
			
			View.PlayDesire(desire, filled);
		}
		#endregion
	}
}