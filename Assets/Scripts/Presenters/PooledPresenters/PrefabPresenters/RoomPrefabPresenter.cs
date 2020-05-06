using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;

namespace Lunra.WildVacuum.Presenters
{
	public class RoomPrefabPresenter : PrefabPresenter<RoomPrefabModel, RoomPrefabView>
	{
		public RoomPrefabPresenter(GameModel game, RoomPrefabModel model) : base(game, model) { }
	}
}