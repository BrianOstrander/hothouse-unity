using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;

namespace Lunra.Hothouse.Presenters
{
	public class FloraPresenter : BaseFloraPresenter<FloraView>
	{
		public FloraPresenter(GameModel game, FloraModel model) : base(game, model) { }
	}
}