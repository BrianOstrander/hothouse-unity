using Lunra.Hothouse.Ai.Bubbler;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class BubblerPresenter : AgentPresenter<BubblerModel, GenericAgentView, BubblerStateMachine>
	{
		public BubblerPresenter(GameModel game, BubblerModel model) : base(game, model) { }

		protected override void OnPosition(Vector3 position)
		{
			base.OnPosition(position);
			
			Model.RecalculateEntrances(position);
		}
	}
}