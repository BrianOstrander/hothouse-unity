using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class FloraPoolModel : ClearablePoolModel<FloraModel>
	{
		struct SpeciesInfo
		{
			public FloatRange AgeDuration;
			public FloatRange ReproductionDuration;
			public FloatRange ReproductionRadius;
			public int ReproductionFailureLimit;
			public float HealthMaximum;
			public float SpreadDamage;
			public bool AttacksBuildings;
			public Func<Inventory> GenerateDrops;
			public string[] ValidPrefabIds;

			public SpeciesInfo(
				FloatRange ageDuration,
				FloatRange reproductionDuration,
				FloatRange reproductionRadius,
				int reproductionFailureLimit,
				float healthMaximum,
				float spreadDamage,
				bool attacksBuildings,
				Func<Inventory> generateDrops,
				string[] validPrefabIds
			)
			{
				AgeDuration = ageDuration;
				ReproductionDuration = reproductionDuration;
				ReproductionRadius = reproductionRadius;
				ReproductionFailureLimit = reproductionFailureLimit;
				HealthMaximum = healthMaximum;
				SpreadDamage = spreadDamage;
				AttacksBuildings = attacksBuildings;
				GenerateDrops = generateDrops;
				ValidPrefabIds = validPrefabIds;
			}
		}

		static class Defaults
		{
			public const int ReproductionFailureLimit = 40;

			public static Func<Inventory> GenerateDropsEmpty() => () => Inventory.Empty;
			
			public static Func<Inventory> GenerateDrops(
				Inventory.Types type,
				int weight = 1
			)
			{
				return () => new Inventory(
					new Dictionary<Inventory.Types, int>
					{
						{ type , weight }
					}
				);
			}
		}

		static readonly Dictionary<FloraSpecies, SpeciesInfo> Infos = new Dictionary<FloraSpecies, SpeciesInfo>
		{
			{
				FloraSpecies.Grass,
				new SpeciesInfo(
					new FloatRange(30f, 60f), 
					new FloatRange(30f, 60f),
					new FloatRange(0.5f, 1f),
					Defaults.ReproductionFailureLimit,
					100f,
					50f,
					false,
					Defaults.GenerateDrops(Inventory.Types.Stalks),
					new []
					{
						"grass0",
						"grass1"
					}
				)
			},
			{
				FloraSpecies.Shroom,
				new SpeciesInfo(
					new FloatRange(4f, 16f), 
					new FloatRange(10f, 20f), 
					new FloatRange(0.75f, 1.25f),
					Defaults.ReproductionFailureLimit,
					100f,
					50f,
					true,
					Defaults.GenerateDrops(Inventory.Types.Stalks),
					new []
					{
						"shroom0"
					}
				)
			},
			{
				FloraSpecies.Wheat,
				new SpeciesInfo(
					new FloatRange(10f, 20f), 
					new FloatRange(120f, 200f), 
					new FloatRange(0.5f, 1f),
					Defaults.ReproductionFailureLimit,
					100f,
					0f,
					false,
					Defaults.GenerateDrops(Inventory.Types.Rations),
					new []
					{
						"wheat0"
					}
				)
			},
			{
				FloraSpecies.SeekerSpawner,
				new SpeciesInfo(
					new FloatRange(4f, 16f), 
					// new FloatRange(10f, 20f),
					new FloatRange(1f, 2f),
					new FloatRange(0.75f, 1.25f),
					Defaults.ReproductionFailureLimit,
					100f,
					50f,
					true,
					Defaults.GenerateDrops(Inventory.Types.Stalks),
					new []
					{
						"seeker_spawner_0"
					}
				)
			},
		};

		GameModel game;
		
		public override void Initialize(GameModel game)
		{
			this.game = game;
			Initialize(OnInitializePresenter);
		}

		void OnInitializePresenter(FloraModel model)
		{
			switch (model.Species.Value)
			{
				case FloraSpecies.Grass:
				case FloraSpecies.Wheat:
				case FloraSpecies.Shroom:
					new FloraPresenter(game, model);
					break;
				case FloraSpecies.SeekerSpawner:
					new SeekerSpawnerFloraPresenter(game, model);
					break;
				default:
					Debug.LogError("Unrecognized Species: "+model.Species.Value);
					break;
			}
		}

		public FloraModel ActivateChild(
			FloraSpecies species,
			string roomId,
			Vector3 position
		)
		{
			var info = Infos[species];
			var result = Activate(
				info.ValidPrefabIds.Random(),
				roomId,
				position,
				RandomRotation,
				model => Reset(model, species, info)
			);
			if (IsInitialized) game.LastLightUpdate.Value = game.LastLightUpdate.Value.SetSensitiveStale(result.Id.Value);
			return result;
		}
		
		public FloraModel ActivateAdult(
			FloraSpecies species,
			string roomId,
			Vector3 position
		)
		{
			var info = Infos[species];
			var result = Activate(
				info.ValidPrefabIds.Random(),
				roomId,
				position,
				RandomRotation,
				model =>
				{
					Reset(model, species, info);
					model.Age.Value = model.Age.Value.Done();
				}
			);
			if (IsInitialized) game.LastLightUpdate.Value = game.LastLightUpdate.Value.SetSensitiveStale(result.Id.Value);
			return result;
		}
		
		Quaternion RandomRotation => Quaternion.AngleAxis(DemonUtility.GetNextFloat(0f, 360f), Vector3.up);

		void Reset(
			FloraModel model,
			FloraSpecies species,
			SpeciesInfo info
		)
		{
			base.Reset(model);
			
			model.Species.Value = species;
			model.Age.Value = Interval.WithMaximum(info.AgeDuration.Evaluate(DemonUtility.NextFloat));
			model.ReproductionElapsed.Value = Interval.WithMaximum(info.ReproductionDuration.Evaluate(DemonUtility.NextFloat));
			model.ReproductionRadius.Value = info.ReproductionRadius;
			model.ReproductionFailures.Value = 0;
			model.ReproductionFailureLimit.Value = info.ReproductionFailureLimit;
			model.SpreadDamage.Value = info.SpreadDamage;
			model.AttacksBuildings.Value = info.AttacksBuildings;
			model.Health.ResetToMaximum(info.HealthMaximum);
			model.Clearable.ItemDrops.Value = info.GenerateDrops();
		}
	}
}