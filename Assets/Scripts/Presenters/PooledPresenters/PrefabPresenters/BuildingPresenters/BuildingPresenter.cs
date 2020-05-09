using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;

namespace Lunra.Hothouse.Presenters
{
	public class BuildingPresenter : BaseBuildingPresenter<BuildingModel, BuildingView>
	{
		public BuildingPresenter(GameModel game, BuildingModel model) : base(game, model) { }
		
		
	}
}