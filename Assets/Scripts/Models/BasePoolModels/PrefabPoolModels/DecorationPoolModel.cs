using Lunra.Hothouse.Presenters;
using Lunra.Hothouse.Views;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class DecorationPoolModel : BasePrefabPoolModel<DecorationModel>
	{
		public override void Initialize(GameModel game)
		{
			Initialize(
				m => new PrefabPresenter<DecorationModel, DecorationView>(game, m)
			);
		}

		public DecorationModel Activate(
			string prefabId,
			string roomId,
			Vector3 position,
			Quaternion rotation	
		)
		{
			return base.Activate(
				prefabId,
				roomId,
				position,
				rotation
			);
		}
	}
}