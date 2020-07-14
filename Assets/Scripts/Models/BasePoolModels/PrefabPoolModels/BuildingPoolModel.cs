using System;
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
		
		public struct DesiredInventoryInfo
		{
			public static DesiredInventoryInfo NewInActive()
			{
				return new DesiredInventoryInfo(
					Types.InActive,
					Inventory.Empty
				);
			}
			
			public static DesiredInventoryInfo NewCapacity()
			{
				return new DesiredInventoryInfo(
					Types.Capacity,
					Inventory.Empty
				);
			}
			
			public static DesiredInventoryInfo NewSpecified(Inventory specified)
			{
				return new DesiredInventoryInfo(
					Types.Specified,
					specified
				);
			}
			
			public enum Types
			{
				Unknown = 0,
				InActive = 10,
				Capacity = 20,
				Specified = 30
			}

			public readonly Types Type;
			public readonly Inventory Specified;
			
			public DesiredInventoryInfo(
				Types type,
				Inventory specified
			)
			{
				Type = type;
				Specified = specified;
			}
		}
		
		struct BuildingInfo
		{
		
			public BuildingTypes BuildingType;
			public float HealthMaximum;
			public Inventory Inventory;
			public DesiredInventoryInfo DesiredInventory;
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
				BuildingTypes buildingType,
				float healthMaximum,
				Inventory inventory,
				DesiredInventoryInfo desiredInventory,
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
				BuildingType = buildingType;
				HealthMaximum = healthMaximum;
				Inventory = inventory;
				DesiredInventory = desiredInventory;
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

		static readonly Dictionary<string, BuildingInfo> Infos = new Dictionary<string, BuildingInfo>
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
				BuildingNames.Stockpiles.StartingWagon,
				new BuildingInfo(
					BuildingTypes.Stockpile,
					100f,
					Inventory.Empty,
					DesiredInventoryInfo.NewInActive(), 
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Rations , 25 },
								{ Inventory.Types.StalkDry , 25 },
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
								{ Inventory.Types.StalkDry , 10 }
							}
						)	
					),
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.StalkDry , 4 }
						}
					), 
					Inventory.Empty, 
					Interval.Zero(),
					LightStates.Unknown,
					new DesireQuality[]
					{
						// DesireQuality.New(
						// 	Desires.Eat,
						// 	1f,
						// 	new Inventory(
						// 		new Dictionary<Inventory.Types, int>
						// 		{
						// 			{ Inventory.Types.Rations , 1 }
						// 		}
						// 	)
						// ) 
					},
					2,
					new []
					{
						"starting_wagon"
					}
				)
			},
			{
				BuildingNames.Lights.Bonfire,
				new BuildingInfo(
					BuildingTypes.Light,
					100f,
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.StalkDry , Constants.BonfireStalkCost }
						}
					), 
					DesiredInventoryInfo.NewCapacity(),
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.StalkDry , Constants.BonfireStalkCost }
							}
						)	
					),
					InventoryPermission.DepositForJobs(Jobs.Laborer),
					Constants.LightSourcePlacementLightRequirement,
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.StalkDry , Constants.BonfireStalkCost }
							}
						)	
					),
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.StalkDry , 1 }
						}
					), 
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.StalkDry , 1 }
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
				BuildingNames.Beds.Bedroll,
				new BuildingInfo(
					BuildingTypes.Bed,
					100f,
					Inventory.Empty, 
					DesiredInventoryInfo.NewInActive(),
					InventoryCapacity.None(),
					InventoryPermission.NoneForAnyJob(),
					Constants.DefaultPlacementLightRequirement,
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.StalkDry , 2 }
							}
						)	
					),
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.StalkDry , 1 }
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
				BuildingNames.Barricades.Small,
				new BuildingInfo(
					BuildingTypes.Barricade,
					100f,
					Inventory.Empty,
					DesiredInventoryInfo.NewInActive(), 
					InventoryCapacity.None(),
					InventoryPermission.NoneForAnyJob(),
					Constants.DefaultPlacementLightRequirement,
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.StalkDry , 4 }
							}
						)	
					),
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.StalkDry , 1 }
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
				BuildingNames.Stockpiles.SmallDepot,
				new BuildingInfo(
					BuildingTypes.Stockpile,
					100f,
					Inventory.Empty,
					DesiredInventoryInfo.NewInActive(),
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Rations , 25 },
								{ Inventory.Types.StalkDry , 25 },
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
								{ Inventory.Types.StalkDry , 10 }
							}
						)	
					),
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.StalkDry , 4 }
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
					2,
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
			string buildingName,
			string roomId,
			Vector3 position,
			Quaternion rotation,
			BuildingStates buildingState
		)
		{
			var info = Infos[buildingName];
			
			var result = Activate(
				info.ValidPrefabIds.Random(),
				roomId,
				position,
				rotation,
				model => Reset(
					model,
					buildingName,
					info,
					buildingState
				)
			);

			if (IsInitialized) game.LastLightUpdate.Value = game.LastLightUpdate.Value.SetSensitiveStale(result.Id.Value);
			return result;
		}

		void Reset(
			BuildingModel model,
			string buildingName,
			BuildingInfo info,
			BuildingStates buildingState
		)
		{
			model.BuildingName.Value = buildingName;
			model.Type.Value = info.BuildingType;

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

			var desiredInventory = InventoryDesire.None();

			switch (info.DesiredInventory.Type)
			{
				
				case DesiredInventoryInfo.Types.InActive:
					break;
				case DesiredInventoryInfo.Types.Capacity:
					desiredInventory = InventoryDesire.UnCalculated(info.InventoryCapacity.GetMaximum());
					break;
				case DesiredInventoryInfo.Types.Specified:
					desiredInventory = InventoryDesire.UnCalculated(info.DesiredInventory.Specified);
					break;
				default:
					Debug.LogError("Unrecognized DesiredInventory.Type: " + info.DesiredInventory.Type);
					break;
			}
			
			model.Inventory.Reset(
				info.InventoryPermission,
				info.InventoryCapacity,
				desiredInventory
			);

			model.Inventory.Add(info.Inventory);

			model.ConstructionInventory.Reset(
				InventoryPermission.DepositForJobs(Jobs.Stockpiler), 
				info.ConstructionInventoryCapacity
			);
			
			model.SalvageInventory.Reset(
				InventoryPermission.WithdrawalForJobs(Jobs.Stockpiler),
				InventoryCapacity.ByIndividualWeight(info.SalvageInventory)
			);

			model.SalvageInventory.Add(info.SalvageInventory);
			
			model.Obligations.Reset();
		}
	}
}