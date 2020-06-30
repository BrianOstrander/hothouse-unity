using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class DwellerPoolModel : BasePrefabPoolModel<DwellerModel>
	{
		static readonly string[] ValidPrefabIds = new[]
		{
			"default"
		};

		GameModel game;
		
		public override void Initialize(GameModel game)
		{
			this.game = game;
			Initialize(
				model => new DwellerPresenter(game, model)	
			);
		}

		public DwellerModel Activate(
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

		void Reset(DwellerModel model)
		{
			// Agent Properties
			// TODO: NavigationPlan and others may need to be reset...
			model.NavigationVelocity.Value = 4f;
			model.IsDebugging = false;
			model.NavigationForceDistanceMaximum.Value = 4f;
			model.Health.ResetToMaximum(100f);
			model.Inventory.Reset(InventoryCapacity.ByTotalWeight(2));
			
			// Dweller Properties
			model.Job.Value = Jobs.None;
			// model.JobShift.Value = new DayTimeFrame(0.0f, 0.75f);
			model.JobShift.Value = DayTimeFrame.Maximum;
			model.Desire.Value = Desires.None;
			model.MeleeRange.Value = 0.75f;
			model.MeleeCooldown.Value = 0.5f;
			model.MeleeDamage.Value = 60f;

			model.WithdrawalCooldown.Value = 0.5f;
			model.DepositCooldown.Value = model.WithdrawalCooldown.Value;
			model.TransferDistance.Value = 0.75f;
			
			model.DesireDamage.Value = new Dictionary<Desires, float>
			{
				{ Desires.Eat , 0.3f },
				{ Desires.Sleep , 0.1f }
			};
			model.DesireMissedEmoteTimeout.Value = 2;

			model.LowRationThreshold.Value = 1;
			model.ObligationDistance.Value = 0.75f;
			model.ObligationMinimumConcentrationDuration.Value = 0.5f;
			
			model.InventoryPromises.Reset();
		}
	}
}