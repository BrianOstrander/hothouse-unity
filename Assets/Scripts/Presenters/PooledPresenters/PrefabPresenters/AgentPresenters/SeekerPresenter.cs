using Lunra.Hothouse.Ai.Dweller;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;

namespace Lunra.Hothouse.Presenters
{
	public class SeekerPresenter : AgentPresenter<DwellerModel, SeekerView, DwellerStateMachine>
	{
		public SeekerPresenter(GameModel game, DwellerModel model) : base(game, model) { }

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