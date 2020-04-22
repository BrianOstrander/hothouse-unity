using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;

namespace Lunra.WildVacuum.Presenters
{
	public class RoomPrefabPresenter : PrefabPresenter<RoomPrefabView, RoomPrefabModel>
	{
		public RoomPrefabPresenter(GameModel game, RoomPrefabModel prefab) : base(game, prefab) { }
	}
}