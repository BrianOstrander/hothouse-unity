using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.NumberDemon;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class FloraPoolModel : BasePrefabPoolModel<FloraModel>
	{
		public struct SpeciesData
		{
			public FloraSpecies Species;
			public FloatRange AgeDuration;
			public FloatRange ReproductionDuration;
			public FloatRange ReproductionRadius;
			public int ReproductionFailureLimit;
			public float HealthMaximum;
			public float SpreadDamage;
			public bool AttacksBuildings;
			public Func<Inventory> GenerateDrops;
			public int CountPerRoomMinimum;
			public int CountPerRoomMaximum;
			public float SpawnDistanceNormalizedMinimum;
			public int CountPerClusterMinimum;
			public int CountPerClusterMaximum;
			public bool RequiredInSpawn;
			public bool AllowedInSpawn;
			public string[] ValidPrefabIds;

			public SpeciesData(
				FloraSpecies species,
				FloatRange ageDuration,
				FloatRange reproductionDuration,
				FloatRange reproductionRadius,
				int reproductionFailureLimit,
				float healthMaximum,
				float spreadDamage,
				bool attacksBuildings,
				Func<Inventory> generateDrops,
				int countPerRoomMinimum,
				int countPerRoomMaximum,
				float spawnDistanceNormalizedMinimum,
				int countPerClusterMinimum,
				int countPerClusterMaximum,
				bool requiredInSpawn,
				bool allowedInSpawn,
				string[] validPrefabIds
			)
			{
				Species = species;
				AgeDuration = ageDuration;
				ReproductionDuration = reproductionDuration;
				ReproductionRadius = reproductionRadius;
				ReproductionFailureLimit = reproductionFailureLimit;
				HealthMaximum = healthMaximum;
				SpreadDamage = spreadDamage;
				AttacksBuildings = attacksBuildings;
				GenerateDrops = generateDrops;
				CountPerRoomMinimum = countPerRoomMinimum;
				CountPerRoomMaximum = countPerRoomMaximum;
				SpawnDistanceNormalizedMinimum = spawnDistanceNormalizedMinimum;
				CountPerClusterMinimum = countPerClusterMinimum;
				CountPerClusterMaximum = countPerClusterMaximum;
				RequiredInSpawn = requiredInSpawn;
				AllowedInSpawn = allowedInSpawn;
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

		readonly SpeciesData[] data =
		{
			new SpeciesData(
				FloraSpecies.Grass,
				new FloatRange(30f, 60f), 
				new FloatRange(30f, 60f),
				new FloatRange(0.5f, 1f),
				Defaults.ReproductionFailureLimit,
				100f,
				50f,
				false,
				Defaults.GenerateDrops(Inventory.Types.Stalks),
				0,
				4,
				0f,
				40,
				60,
				true,
				true,
				new []
				{
					"grass0",
					"grass1"
				}
			),
			new SpeciesData(
				FloraSpecies.Shroom,
				new FloatRange(4f, 16f), 
				new FloatRange(10f, 20f), 
				new FloatRange(0.75f, 1.25f),
				Defaults.ReproductionFailureLimit,
				100f,
				50f,
				true,
				Defaults.GenerateDrops(Inventory.Types.Stalks),
				0,
				1,
				0.5f,
				1,
				2,
				false,
				false,
				new []
				{
					"shroom0"
				}
			),
			new SpeciesData(
				FloraSpecies.Wheat,
				new FloatRange(10f, 20f), 
				new FloatRange(120f, 200f), 
				new FloatRange(0.5f, 1f),
				Defaults.ReproductionFailureLimit,
				100f,
				0f,
				false,
				Defaults.GenerateDrops(Inventory.Types.Rations),
				0,
				6,
				0f,
				30,
				40,
				true,
				true,
				new []
				{
					"wheat0"
				}
			),
			new SpeciesData(
				FloraSpecies.SeekerSpawner,
				new FloatRange(4f, 16f), 
				// new FloatRange(10f, 20f),
				new FloatRange(1f, 2f),
				new FloatRange(0.75f, 1.25f),
				Defaults.ReproductionFailureLimit,
				100f,
				50f,
				true,
				Defaults.GenerateDrops(Inventory.Types.Stalks),
				1,
				1,
				0f,
				1,
				2,
				false,
				false,
				new []
				{
					"seeker_spawner_0"
				}
			)
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
			var data = this.data.First(d => d.Species == species);
			var result = Activate(
				data.ValidPrefabIds.Random(),
				roomId,
				position,
				RandomRotation,
				model => Reset(model, species, data)
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
			var data = this.data.First(d => d.Species == species);
			var result = Activate(
				data.ValidPrefabIds.Random(),
				roomId,
				position,
				RandomRotation,
				model =>
				{
					Reset(model, species, data);
					model.Age.Value = model.Age.Value.Done();
				}
			);
			if (IsInitialized) game.LastLightUpdate.Value = game.LastLightUpdate.Value.SetSensitiveStale(result.Id.Value);
			return result;
		}

		public SpeciesData[] GetValidSpeciesData(RoomModel room)
		{
			if (room.IsSpawn.Value)
			{
				return data
					.Where(d => d.RequiredInSpawn || d.AllowedInSpawn)
					.ToArray();
			}

			return data
				.Where(d => d.SpawnDistanceNormalizedMinimum <= room.SpawnDistanceNormalized.Value)
				.ToArray();
		}

		public SpeciesData GetSpeciesData(FloraSpecies species) => data.First(d => d.Species == species);
		
		Quaternion RandomRotation => Quaternion.AngleAxis(DemonUtility.GetNextFloat(0f, 360f), Vector3.up);

		void Reset(
			FloraModel model,
			FloraSpecies species,
			SpeciesData data
		)
		{
			model.Clearable.Reset();
			
			model.Species.Value = species;
			model.Age.Value = Interval.WithMaximum(data.AgeDuration.Evaluate(DemonUtility.NextFloat));
			model.ReproductionElapsed.Value = Interval.WithMaximum(data.ReproductionDuration.Evaluate(DemonUtility.NextFloat));
			model.ReproductionRadius.Value = data.ReproductionRadius;
			model.ReproductionFailures.Value = 0;
			model.ReproductionFailureLimit.Value = data.ReproductionFailureLimit;
			model.SpreadDamage.Value = data.SpreadDamage;
			model.AttacksBuildings.Value = data.AttacksBuildings;
			model.Health.ResetToMaximum(data.HealthMaximum);
			model.Clearable.ItemDrops.Value = data.GenerateDrops();
		}
	}
}