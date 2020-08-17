using Lunra.Hothouse.Ai.SnapCap;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class SnapCapPresenter : AgentPresenter<SnapCapModel, GenericAgentView, SnapCapStateMachine>
	{
		public SnapCapPresenter(GameModel game, SnapCapModel model) : base(game, model) { }

		protected override void OnPosition(Vector3 position)
		{
			base.OnPosition(position);
			
			Model.RecalculateEntrances(position);
		}
	}
}