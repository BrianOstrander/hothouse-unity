using Lunra.Hothouse.Models;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Presenters
{
	public class SeekerSpawnerFloraPresenter : BaseFloraPresenter<FloraView>
	{
		protected override ReproductionEvents DefaultReproductionEvent => ReproductionEvents.Custom;

		public SeekerSpawnerFloraPresenter(GameModel game, FloraModel model) : base(game, model) { }

		protected override void OnReproductionCustom(
			string roomId,
			Vector3 position
		)
		{
			Game.Seekers.Activate(
				roomId,
				position
			);
		}
	}
}