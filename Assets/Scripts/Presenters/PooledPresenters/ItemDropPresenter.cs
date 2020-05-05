using System.Linq;
using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;
using UnityEngine;

namespace Lunra.WildVacuum.Presenters
{
	public class ItemDropPresenter : PooledPresenter<ItemDropView, ItemDropModel>
	{
		public ItemDropPresenter(GameModel game, ItemDropModel model) : base(game, model) { }

		protected override void OnBind()
		{
			base.OnBind();
		}

		protected override void OnUnBind()
		{
			base.OnUnBind();
		}

		protected override void OnShow()
		{
			var item = Model.Inventory.Value.Current.OrderByDescending(i => i.Count).FirstOrDefault();
			View.SetEntry(item.Count, item.Type);
		}
	}
}