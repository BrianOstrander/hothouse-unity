using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class SeekerPoolModel : BasePrefabPoolModel<SeekerModel>
	{
		static readonly string[] ValidPrefabIds = {
			"default"
		};

		GameModel game;
		
		public override void Initialize(GameModel game)
		{
			this.game = game;
			Initialize(
				model => new SeekerPresenter(game, model)	
			);
		}

		public SeekerModel Activate(
			string roomId,
			Vector3 position
		)
		{
			var result = Activate(
				ValidPrefabIds.Random(),
				roomId,
				position,
				RandomRotation,
				model => Reset(model)
			);
			return result;
		}
		
		Quaternion RandomRotation => Quaternion.AngleAxis(DemonUtility.GetNextFloat(0f, 360f), Vector3.up);

		void Reset(SeekerModel model)
		{
			model.NavigationVelocity.Value = 4f;
			model.IsDebugging = false;
			model.NavigationForceDistanceMaximum.Value = 4f;
			model.Health.ResetToMaximum(100f);

			model.InventoryCapacity.Value = InventoryCapacity.ByTotalWeight(2);
		}
	}
}