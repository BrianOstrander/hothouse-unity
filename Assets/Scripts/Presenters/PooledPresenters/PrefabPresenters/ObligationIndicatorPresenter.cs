using System.Linq;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class ObligationIndicatorPresenter : PrefabPresenter<ObligationIndicatorModel, ObligationIndicatorView>
	{
		public ObligationIndicatorPresenter(GameModel game, ObligationIndicatorModel model) : base(game, model) { }

		protected override void Bind()
		{
			Model.TargetInstance.Obligations.All.Changed += OnTargetInstanceObligations;
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Model.TargetInstance.Obligations.All.Changed -= OnTargetInstanceObligations;
			
			base.UnBind();
		}
		
		#region TargetInstance Events
		void OnTargetInstanceObligations(Obligation[] obligations)
		{
			var obligation = Model.ObligationInstance;

			if (!obligation.IsValid)
			{
				Model.PooledState.Value = PooledStates.InActive;
				return;
			}
			
			UpdateObligation();
		}
		#endregion

		#region View Events
		
		protected override void OnViewPrepare() => UpdateObligation();
		#endregion

		#region Utility
		void UpdateObligation()
		{
			if (View.NotVisible) return;
			
			View.SetAction(Model.ObligationInstance.Type.Action);
		}
		#endregion
	}
}