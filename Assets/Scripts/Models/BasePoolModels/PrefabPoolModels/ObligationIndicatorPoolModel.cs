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
				else model.TargetInstance = target;
			}
			
			Initialize(
				model => new ObligationIndicatorPresenter(game, model)	
			);
		}

		public ObligationIndicatorModel Activate(
			string obligationId,
			IObligationModel target
		)
		{
			var result = AllActive.FirstOrDefault(o => o.ObligationId.Value == obligationId);
			if (result != null)
			{
				return result;
			}

			var obligation = target.Obligations.All.Value.FirstOrDefault(o => o.Id == obligationId);

			if (!obligation.IsValid)
			{
				Debug.LogError("Unable to find an obligation on " + target.Id.Value + " with id: " + obligationId);
				return null;
			}
			
			result = Activate(
				obligation.Type.PrefabId,
				target.RoomTransform.Id.Value,
				target.Transform.Position.Value,
				target.Transform.Rotation.Value,
				model => Reset(
					model,
					obligationId,
					target
				)
			);

			return result;
		}

		void Reset(
			ObligationIndicatorModel model,
			string obligationId,
			IObligationModel target
		)
		{
			model.ObligationId.Value = obligationId;
			model.TargetInstance = target;
		}
	}
}