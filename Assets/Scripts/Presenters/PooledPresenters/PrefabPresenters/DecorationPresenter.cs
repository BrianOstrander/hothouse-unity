using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class DecorationPresenter : PrefabPresenter<DecorationModel, DecorationView>
	{
		public DecorationPresenter(GameModel game, DecorationModel model) : base(game, model) { }

		protected override void Bind()
		{
			Model.PossibleEntrance.Value = Model.Transform.Position.Value + ((Model.Transform.Rotation.Value * Vector3.forward) * (View.ExtentForward + DecorationView.Constants.Boundaries.PossibleEntranceOffset));

			Model.Flow.Changed += OnDecorationFlow; 
			
			base.Bind();
		}

		protected override void UnBind()
		{
			Model.Flow.Changed -= OnDecorationFlow;
			
			base.UnBind();
		}

		#region DecorationModel Events
		void OnDecorationFlow(float flow) => View.Flow = flow;
		#endregion
	}
}