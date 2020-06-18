using Lunra.Hothouse.Ai.Dweller;
using Lunra.Hothouse.Ai.Seeker;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;

namespace Lunra.Hothouse.Presenters
{
	public class SeekerPresenter : AgentPresenter<SeekerModel, SeekerView, SeekerStateMachine>
	{
		public SeekerPresenter(GameModel game, SeekerModel model) : base(game, model) { }

		protected override void Bind()
		{
			
			
			base.Bind();
		}

		protected override void UnBind()
		{
			
			
			base.UnBind();
		}
	}
}