using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Presenters;
using Lunra.WildVacuum.Views;

namespace System
{
	public class DesireBuildingPresenter : BuildingPresenter<BuildingView, DesireBuildingModel>
	{
		public DesireBuildingPresenter(GameModel game, DesireBuildingModel model) : base(game, model) { }
	}
}