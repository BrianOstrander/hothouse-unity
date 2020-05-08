using Lunra.WildVacuum.Models;
using Lunra.WildVacuum.Views;

namespace Lunra.WildVacuum.Presenters
{
	public class BuildingPresenter : BaseBuildingPresenter<BuildingModel, BuildingView>
	{
		public BuildingPresenter(GameModel game, BuildingModel model) : base(game, model) { }
		
		
	}
}