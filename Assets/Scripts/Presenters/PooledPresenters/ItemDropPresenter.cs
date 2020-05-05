using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;

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
			
		}

		#region GameModel Events
		#endregion
	}
}