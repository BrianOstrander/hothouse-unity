using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class ObligationIndicatorPoolModel : BasePrefabPoolModel<ObligationIndicatorModel>
	{
		GameModel game;
		
		public override void Initialize(GameModel game)
		{
			this.game = game;

			foreach (var model in AllActive)
			{
				var target = game.GetObligations()
					.FirstOrDefault(m => m.Obligations.All.Value.Any(o => o.Id == model.ObligationId.Value));

				if (target == null) Debug.LogError("Unable to find any target with obligation id: " + model.ObligationId.Value);
				else model.TargetInstance.Value = target;
			}
			
			Initialize(
				model => new ObligationIndicatorPresenter(game, model)	
			);
		}
		
		public ObligationIndicatorModel Register(
			Obligation obligation,
			IObligationModel target
		)
		{
			var result = AllActive.FirstOrDefault(o => o.ObligationId.Value == obligation.Id);
			if (result != null)
			{
				Debug.LogError("Trying to register an obligation but there is already a indicator with the same id: "+obligation.Id);
				return result;
			}

			if (target.Obligations.All.Value.FirstOrDefault(o => o.Id == obligation.Id).IsValid)
			{
				Debug.LogError("Trying to register an obligation but there is already an obligation with the same id on this target: "+obligation.Id);
				return null;
			}

			target.Obligations.All.Value = target.Obligations.All.Value
				.Append(obligation)
				.ToArray();
			
			result = Activate(
				obligation.Type.PrefabId,
				target.RoomTransform.Id.Value,
				target.Transform.Position.Value,
				target.Transform.Rotation.Value,
				model => Reset(
					model,
					obligation,
					target
				)
			);

			return result;
		}

		void Reset(
			ObligationIndicatorModel model,
			Obligation obligation,
			IObligationModel target
		)
		{
			model.ObligationId.Value = obligation.Id;
			model.TargetInstance.Value = target;
		}
	}
}