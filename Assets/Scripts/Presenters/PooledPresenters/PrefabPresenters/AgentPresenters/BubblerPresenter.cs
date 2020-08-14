using Lunra.Hothouse.Ai.Bubbler;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class BubblerPresenter : AgentPresenter<BubblerModel, BubblerView, BubblerStateMachine>
	{
		public BubblerPresenter(GameModel game, BubblerModel model) : base(game, model) { }

		protected override void Bind()
		{
			// HACK MOVE THIS!
			Model.Clearable.State.Changed += state =>
			{
				if (state == ClearableStates.Marked) Model.Obligations.Add(ObligationCategories.Destroy.Generic);
			};
			
			Debug.LogError("TODO: GET RID OF LIGHT REQ OR ADD IT HERE...");
			
			base.Bind();
		}

		protected override void OnPosition(Vector3 position)
		{
			base.OnPosition(position);
			
			Model.RecalculateEntrances(position);
		}
	}
}