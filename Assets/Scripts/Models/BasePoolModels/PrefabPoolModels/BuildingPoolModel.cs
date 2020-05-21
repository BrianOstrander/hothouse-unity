using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class BuildingPoolModel : BasePrefabPoolModel<BuildingModel>
	{
		struct BuildingInfo
		{
			public Inventory Inventory;
			public InventoryCapacity InventoryCapacity;
			public InventoryPermission InventoryPermission;
			public InventoryCapacity ConstructionInventoryCapacity;
			public Inventory SalvageInventory;
			public Inventory LightFuel;
			public Interval LightFuelInterval;
			public LightStates LightStateDefault;
			public DesireQuality[] DesireQualities;
			public string[] ValidPrefabIds;

			public BuildingInfo(
				Inventory inventory,
				InventoryCapacity inventoryCapacity,
				InventoryPermission inventoryPermission,
				InventoryCapacity constructionInventoryCapacity,
				Inventory salvageInventory,
				Inventory lightFuel,
				Interval lightFuelInterval,
				LightStates lightStateDefault,
				DesireQuality[] desireQualities,
				string[] validPrefabIds
			)
			{
				Inventory = inventory;
				InventoryCapacity = inventoryCapacity;
				InventoryPermission = inventoryPermission;
				ConstructionInventoryCapacity = constructionInventoryCapacity;
				SalvageInventory = salvageInventory;
				LightFuel = lightFuel;
				LightFuelInterval = lightFuelInterval;
				LightStateDefault = lightStateDefault;
				DesireQualities = desireQualities;
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
					Inventory.Empty, 
					Interval.Zero(),
					LightStates.Unknown,
					new []
					{
						DesireQuality.New(Desires.Eat, 1f) 
					},
					new []
					{
						"starting_wagon"
					}
				)
			},
			{
				Buildings.Bonfire,
				new BuildingInfo(
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Stalks , 4 }
						}
					), 
					InventoryCapacity.ByIndividualWeight(
						new Inventory(
							new Dictionary<Inventory.Types, int>
							{
								{ Inventory.Types.Stalks , 4 }
							}
						)	
					),
					InventoryPermission.DepositForJobs(Jobs.Stoker), 
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
					new Inventory(
						new Dictionary<Inventory.Types, int>
						{
							{ Inventory.Types.Stalks , 1 }
						}
					), 
					Interval.WithMaximum(10f),
					LightStates.Fueled,
					new []
					{
						DesireQuality.New(Desires.Warmup, 1f) 
					},
					new []
					{
						"fire_bonfire"
					}
				)
			},
			{
			Buildings.Bedroll,
			new BuildingInfo(
				Inventory.Empty, 
				InventoryCapacity.None(),
				InventoryPermission.NoneForAnyJob(), 
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
				new []
				{
					"bed_bedroll"
				}
			)
		}
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
				model => Reset(model, info, buildingState)
			);
			if (IsInitialized) game.LastLightUpdate.Value = game.LastLightUpdate.Value.SetSensitiveStale(result.Id.Value);
			return result;
		}

		void Reset(
			BuildingModel model,
			BuildingInfo info,
			BuildingStates buildingState
		)
		{
			model.Inventory.Value = info.Inventory;
			model.InventoryCapacity.Value = info.InventoryCapacity;
			model.InventoryPermission.Value = info.InventoryPermission;
			model.ConstructionInventoryCapacity.Value = info.ConstructionInventoryCapacity;
			model.SalvageInventory.Value = info.SalvageInventory;
			model.LightFuel.Value = info.LightFuel;
			model.LightFuelInterval.Value = info.LightFuelInterval;
			model.LightState.Value = info.LightStateDefault;
			model.DesireQualities.Value = info.DesireQualities;
			model.BuildingState.Value = buildingState;
			
			model.IsLightRefueling.Value = true;
			model.LightLevel.Value = 0f;
		}
	}
}