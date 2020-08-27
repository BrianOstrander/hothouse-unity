using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using Lunra.Satchel;
using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class ClearablePresenter<M, V> : PrefabPresenter<M, V>
		where M : IClearableModel
		where V : class, IClearableView
	{
		public ClearablePresenter(GameModel game, M model) : base(game, model) { }

		protected override void Bind()
		{			
			Model.Clearable.MeleeRangeBonus.Value = View.MeleeRangeBonus;
			
			Model.Obligations.All.Changed += OnObligationAll;
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Model.Obligations.All.Changed -= OnObligationAll;

			base.UnBind();
		}

		protected virtual Stack[] CalculateItemDrops() => Model.Clearable.ItemDrops.Value;

		protected override void OnViewPrepare()
		{
			base.OnViewPrepare();
			
			View.Shown += () => OnObligationAll(Model.Obligations.All.Value);
			
			Model.RecalculateEntrances(View);
		}

		#region ClearableModel Events
		void OnObligationAll(ObligationComponent.State state)
		{
			if (View.NotVisible) return;

			if (Model.Obligations.HasAny(ObligationCategories.Destroy.Generic))
			{
				View.Select();
			}
			else
			{
				View.Deselect();
			}
		}
		#endregion

		#region Utility
		protected override bool QueueNavigationCalculation => 0 < Model.Clearable.MeleeRangeBonus.Value;
		#endregion
	}
}