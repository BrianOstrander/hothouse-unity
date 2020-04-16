using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;

namespace Lunra.WildVacuum.Presenters
{
    public class DoorPrefabPresenter : PrefabPresenter<DoorPrefabView, DoorPrefabModel>
    {
        public DoorPrefabPresenter(GameModel game, DoorPrefabModel prefab) : base(game, prefab) { }
    }
}