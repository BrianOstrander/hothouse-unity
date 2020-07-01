using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class BuildingPoolModel : BasePrefabPoolModel<BuildingModel>
	{
		static class Constants
		{
			public const int BonfireStalkCost = 2;
			// public static readonly FloatRange DefaultPlacementLightRequirement = new FloatRange(-1f, 1f);
			public static readonly FloatRange DefaultPlacementLightRequirement = new FloatRange(0.001f, 1f);
			public static readonly FloatRange LightSourcePlacementLightRequirement = new FloatRange(0.001f, 0.33f);
		}
		
		struct BuildingInfo
		{
			public float HealthMaximum;
			public Inventory Inventory;
			public InventoryCapacity InventoryCapacity;
			public InventoryPermission InventoryPermission;
			public FloatRange PlacementLightRequirement;
			public InventoryCapacity ConstructionInventoryCapacity;
			public Inventory SalvageInventory;
			public Inventory LightFuel;
			public Interval LightFuelInterval;
			public LightStates LightStateDefault;
			public DesireQuality[] DesireQualities;
			public int MaximumOwners;
			public string[] ValidPrefabIds;

			public BuildingInfo(
				float healthMaximum,
				Inventory inventory,
				InventoryCapacity inventoryCapacity,
				InventoryPermission inventoryPermission,
				FloatRange placementLightRequirement,
				InventoryCapacity constructionInventoryCapacity,
				Inventory salvageInventory,
				Inventory lightFuel,
				Interval lightFuelInterval,
				LightStates lightStateDefault,
				DesireQuality[] desireQualities,
				int maximumOwners,
				string[] validPrefabIds
			)
			{
				HealthMaximum = healthMaximum;
				Inventory = inventory;
				InventoryCapacity = inventoryCapacity;
				InventoryPermission = inventoryPermission;
				PlacementLightRequirement = placementLightRequirement;
				ConstructionInventoryCapacity = constructionInventoryCapacity;
				SalvageInventory = salvageInventory;
				LightFuel = lightFuel;
				LightFuelInterval = lightFuelInterval;
				LightStateDefault = lightStateDefault;
				DesireQualities = desireQualities;
				MaximumOwners = maximumOwners;
				ValidPrefabIds = validPrefabIds;
			}
		}

		static readonly Dictionary<Buildings, BuildingInfo> Infos = new Dictionary<Buildings, BuildingInfo>
		{
			/*
			{
				Buildings.Unknown,
				new BuildingInfo(
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Rations , 20 }
						}
					), 
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Rations , 100 },
								{ Inventory.Types.Stalks , 100 },
								{ Inventory.Types.Scrap , 100 }
							}
						)	
					),
					InventoryPermission.AllForAnyJob(), 
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Stalks , 10 }
							}
						)	
					),
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Stalks , 4 }
						}
					), 
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Stalks , 1 }
						}
					), 
					Interval.WithMaximum(10f),
					new []
					{
						DesireQuality.New(Desires.Eat, 1f) 
					},
					new []
					{
						"starting_wagon"
					}
				)
			}
			*/
			{
				Buildings.StartingWagon,
				new BuildingInfo(
					100f,
					Inventory.Empty,
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Rations , 100 },
								{ Inventory.Types.Stalks , 100 },
								{ Inventory.Types.Scrap , 100 }
							}
						)	
					),
					InventoryPermission.AllForAnyJob(),
					Constants.DefaultPlacementLightRequirement,
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Stalks , 10 }
							}
						)	
					),
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Stalks , 4 }
						}
					), 
					Inventory.Empty, 
					Interval.Zero(),
					LightStates.Unknown,
					new []
					{
						DesireQuality.New(
							Desires.Eat,
							1f,
							new Inventory(
								new Dictionary<Inventory.Types, int>
								{
									{ Inventory.Types.Rations , 1 }
								}
							)
						) 
					},
					0,
					new []
					{
						"starting_wagon"
					}
				)
			},
			{
				Buildings.Bonfire,
				new BuildingInfo(
					100f,
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Stalks , Constants.BonfireStalkCost }
						}
					), 
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Stalks , Constants.BonfireStalkCost }
							}
						)	
					),
					InventoryPermission.DepositForJobs(Jobs.Laborer),
					Constants.LightSourcePlacementLightRequirement,
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Stalks , Constants.BonfireStalkCost }
							}
						)	
					),
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Stalks , 1 }
						}
					), 
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Stalks , 1 }
						}
					), 
					Interval.WithMaximum(30f),
					LightStates.Fueled,
					new DesireQuality[]
					{
						// DesireQuality.New(Desires.Warmup, 1f) 
					},
					0,
					new []
					{
						"fire_bonfire"
					}
				)
			},
			{
				Buildings.Bedroll,
				new BuildingInfo(
					100f,
					Inventory.Empty, 
					InventoryCapacity.None(),
					InventoryPermission.NoneForAnyJob(),
					Constants.DefaultPlacementLightRequirement,
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Stalks , 2 }
							}
						)	
					),
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Stalks , 1 }
						}
					), 
					Inventory.Empty, 
					Interval.Zero(),
					LightStates.Unknown,
					new []
					{
						DesireQuality.New(Desires.Sleep, 1f) 
					},
					1,
					new []
					{
						"bed_bedroll"
					}
				)
			},
			{
				Buildings.WallSmall,
				new BuildingInfo(
					100f,
					Inventory.Empty, 
					InventoryCapacity.None(),
					InventoryPermission.NoneForAnyJob(),
					Constants.DefaultPlacementLightRequirement,
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Stalks , 4 }
							}
						)	
					),
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Stalks , 1 }
						}
					), 
					Inventory.Empty, 
					Interval.Zero(),
					LightStates.Unknown,
					new DesireQuality[]
					{
						// DesireQuality.New(Desires.Sleep, 1f) 
					},
					0,
					new []
					{
						"wall_small_0"
					}
				)
			},
			{
				Buildings.DepotSmall,
				new BuildingInfo(
					100f,
					Inventory.Empty,
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Rations , 25 },
								{ Inventory.Types.Stalks , 25 },
								{ Inventory.Types.Scrap , 25 }
							}
						)	
					),
					InventoryPermission.AllForAnyJob(),
					Constants.DefaultPlacementLightRequirement,
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Stalks , 10 }
							}
						)	
					),
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Stalks , 4 }
						}
					), 
					Inventory.Empty, 
					Interval.Zero(),
					LightStates.Unknown,
					new []
					{
						DesireQuality.New(
							Desires.Eat,
							1f,
							new Inventory(
								new Dictionary<Inventory.Types, int>
								{
									{ Inventory.Types.Rations , 1 }
								}
							)
						)
					},
					0,
					new []
					{
						"debug_building"
					}
				)
			},
		};

		GameModel game;
		
		public override void Initialize(GameModel game)
		{
			this.game = game;
			Initialize(
				model => new BuildingPresenter(game, model)	
			);
		}

		public BuildingModel Activate(
			Buildings building,
			string roomId,
			Vector3 position,
			Quaternion rotation,
			BuildingStates buildingState
		)
		{
			var info = Infos[building];
			
			var result = Activate(
				info.ValidPrefabIds.Random(),
				roomId,
				position,
				rotation,
				model => Reset(
					model,
					building,
					info,
					buildingState
				)
			);

			if (IsInitialized) game.LastLightUpdate.Value = game.LastLightUpdate.Value.SetSensitiveStale(result.Id.Value);
			return result;
		}

		void Reset(
			BuildingModel model,
			Buildings building,
			BuildingInfo info,
			BuildingStates buildingState
		)
		{
			model.Type.Value = building;

			model.Health.ResetToMaximum(info.HealthMaximum);
			model.PlacementLightRequirement.Value = info.PlacementLightRequirement;
			model.Light.IsLightCalculationsEnabled.Value = false;
			model.Light.LightFuel.Value = info.LightFuel;
			model.Light.LightFuelInterval.Value = info.LightFuelInterval;
			model.Light.LightState.Value = info.LightStateDefault;
			model.DesireQualities.Value = info.DesireQualities;
			model.BuildingState.Value = buildingState;
			
			model.Light.IsLightRefueling.Value = true;
			model.LightSensitive.LightLevel.Value = 0f;
			
			model.Ownership.Reset();
			model.Ownership.MaximumClaimers.Value = info.MaximumOwners;
			
			model.Inventory.Reset(
				info.InventoryPermission,
				info.InventoryCapacity
			);

			model.ConstructionInventory.Reset(
				buildingState == BuildingStates.Constructing ? InventoryPermission.DepositForJobs(Jobs.Laborer) : InventoryPermission.NoneForAnyJob(), 
				info.ConstructionInventoryCapacity
			);
			
			model.SalvageInventory.Reset(
				buildingState == BuildingStates.Salvaging ? InventoryPermission.WithdrawalForJobs(Jobs.Laborer)	: InventoryPermission.NoneForAnyJob(),
				InventoryCapacity.ByIndividualWeight(info.SalvageInventory)
			);

			model.SalvageInventory.Add(info.SalvageInventory);
		}
	}
}