using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Presenters;
using Lunra.WildVacuum.Views;

namespace System
{
	public class ItemCacheBuildingPresenter : BuildingPresenter<ItemCacheBuildingView, ItemCacheBuildingModel>
	{
		public ItemCacheBuildingPresenter(GameModel game, ItemCacheBuildingModel prefab) : base(game, prefab)
		{
			
		}
	}
}