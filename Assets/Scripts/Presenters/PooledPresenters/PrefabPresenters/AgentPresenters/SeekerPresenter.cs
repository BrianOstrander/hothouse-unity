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
			Model.Health.Damaged += OnSeekerHealthDamage;
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Model.Health.Damaged -= OnSeekerHealthDamage;
			
			base.UnBind();
		}
		
		#region SeekerModel Events
		void OnSeekerHealthDamage(Damage.Result result)
		{
			
		}
		#endregion
	}
}