using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.NumberDemon;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class SnapCapPoolModel : BasePrefabPoolModel<SnapCapModel>
	{
		static readonly string[] ValidPrefabIds =
		{
			"snapcap_0"
		};
		
		GameModel game;
		Demon generator = new Demon();
		
		public override void Initialize(GameModel game)
		{
			this.game = game;
			Initialize(
				model => new SnapCapPresenter(game, model)	
			);
		}

		public SnapCapModel Activate(
			string roomId,
			Vector3 position,
			Demon generator = null
		)
		{
			var result = Activate(
				ValidPrefabIds.Random(),
				roomId,
				position,
				Quaternion.AngleAxis((generator ?? this.generator).GetNextFloat(0f, 360f), Vector3.up),
				model => Reset(model, generator ?? this.generator)
			);
			return result;
		}

		void Reset(
			SnapCapModel model,
			Demon generator
		)
		{
			// Agent Properties
			// TODO: NavigationPlan and others may need to be reset...
			model.NavigationVelocity.Value = 60f; // How many meters per day they can walk...
			model.IsDebugging = false;
			model.NavigationForceDistanceMaximum.Value = 4f;
			model.Health.ResetToMaximum(80f);
			model.Inventory.Reset(InventoryCapacity.ByTotalWeight(2));
			model.InteractionRadius.Value = 0.75f;
			
			model.Clearable.Reset(
				Inventory.FromEntry(Inventory.Types.Berries, 6),
				maximumClearers: 3
			);
			model.LightSensitive.Reset();
			
			model.Attacks.Reset(
				new Attack(
					App.M.CreateUniqueId(),
					"melee_generic",
					new FloatRange(0f, 0.5f),
					150f,
					DayTime.FromRealSeconds(2f)
				)
			);

			model.HuntForbiddenExpiration.Value = DayTime.Zero;
			model.HuntRangeMaximum.Value = 6f;
			model.AwakeTime.Value = new DayTimeFrame(0.75f, 0.25f);
			model.NavigationPathMaximum.Value = 3f;
		}
	}
}