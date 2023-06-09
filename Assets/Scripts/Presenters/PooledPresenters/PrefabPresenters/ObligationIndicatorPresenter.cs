using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using UnityEngine.Assertions;

namespace Lunra.Hothouse.Presenters
{
	public class ObligationIndicatorPresenter : PrefabPresenter<ObligationIndicatorModel, ObligationIndicatorView>
	{
		IObligationModel lastTargetInstance;
		
		public ObligationIndicatorPresenter(GameModel game, ObligationIndicatorModel model) : base(game, model) { }

		protected override void Bind()
		{
			Model.TargetInstance.Changed += OnObligationIndicatorTargetInstance; 
			OnObligationIndicatorTargetInstance(Model.TargetInstance.Value);
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Model.TargetInstance.Changed -= OnObligationIndicatorTargetInstance;
			OnObligationIndicatorTargetInstance(null);
			
			base.UnBind();
		}
		
		#region ObligationIndicatorModel Events
		void OnObligationIndicatorTargetInstance(IObligationModel targetInstance)
		{
			Assert.IsFalse(
				!(targetInstance == null && targetInstance == null) && targetInstance == lastTargetInstance,
				"It should not be possible for this event to fire if the current and last instances match: "+targetInstance+" and "+lastTargetInstance
			);
			
			if (lastTargetInstance != null)
			{
				lastTargetInstance.Obligations.All.Changed -= OnTargetInstanceObligations;
			}
			lastTargetInstance = targetInstance;

			if (targetInstance == null)
			{
				Model.Obligation.Value = null;
				Model.PooledState.Value = PooledStates.InActive;
				return;
			}
			
			targetInstance.Obligations.All.Changed += OnTargetInstanceObligations;
		}
		#endregion

		#region TargetInstance Events
		void OnTargetInstanceObligations(ObligationComponent.State obligationState)
		{
			if (!Model.TargetInstance.Value.Obligations.HasAny(Model.Obligation.Value))
			{
				Model.TargetInstance.Value = null;
				return;
			}
			
			UpdateObligation();
		}
		#endregion

		#region View Events
		
		protected override void OnViewPrepare()
		{
			base.OnViewPrepare();

			UpdateObligation();
		}
		#endregion

		#region Utility
		void UpdateObligation()
		{
			if (View.NotVisible) return;
			
			View.SetAction(Model.Obligation.Value.Type);
		}
		#endregion
	}
}