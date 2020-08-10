using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class BuildingModel : PrefabModel,
		ILightModel,
		IHealthModel,
		IConstructionModel,
		IRecipeModel,
		IFarmModel,
		IGoalActivityModel,
		ITagModel
	{
		#region Serialized
		[JsonProperty] string type;
		[JsonIgnore] public ListenerProperty<string> Type { get; }
		
		[JsonProperty] BuildingStates buildingState;
		[JsonIgnore] public ListenerProperty<BuildingStates> BuildingState { get; }

		[JsonProperty] FloatRange placementLightRequirement = FloatRange.Zero;
		[JsonIgnore] public ListenerProperty<FloatRange> PlacementLightRequirement { get; }

		[JsonProperty] public LightComponent Light { get; private set; } = new LightComponent();
		[JsonProperty] public LightSensitiveComponent LightSensitive { get; private set; } = new LightSensitiveComponent();
		[JsonProperty] public BoundaryComponent Boundary { get; private set; } = new BoundaryComponent();
		[JsonProperty] public HealthComponent Health { get; private set; } = new HealthComponent();
		[JsonProperty] public ClaimComponent Ownership { get; private set; } = new ClaimComponent();
		[JsonProperty] public InventoryComponent Inventory { get; private set; } = new InventoryComponent();
		[JsonProperty] public InventoryComponent ConstructionInventory { get; private set; } = new InventoryComponent();
		[JsonProperty] public InventoryComponent SalvageInventory { get; private set; } = new InventoryComponent();
		[JsonProperty] public ObligationComponent Obligations { get; private set; } = new ObligationComponent();
		[JsonProperty] public RecipeComponent Recipes { get; private set; } = new RecipeComponent();
		[JsonProperty] public FarmComponent Farm { get; private set; } = new FarmComponent();
		[JsonProperty] public GoalActivityComponent Activities { get; private set; } = new GoalActivityComponent();
		[JsonProperty] public TagComponent Tags { get; private set; } = new TagComponent();
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public EnterableComponent Enterable { get; } = new EnterableComponent();

		Dictionary<BuildingStates, IBaseInventoryComponent[]> inventoriesByBuildingState = new Dictionary<BuildingStates, IBaseInventoryComponent[]>();
		[JsonIgnore] public IBaseInventoryComponent[] Inventories => inventoriesByBuildingState[BuildingState.Value];
		#endregion

		public bool IsBuildingState(BuildingStates buildingState) => BuildingState.Value == buildingState;
		
		public BuildingModel()
		{
			Type = new ListenerProperty<string>(value => type = value, () => type);
			BuildingState = new ListenerProperty<BuildingStates>(value => buildingState = value, () => buildingState);
			PlacementLightRequirement = new ListenerProperty<FloatRange>(value => placementLightRequirement = value, () => placementLightRequirement);
		
			var emptyInventories = new IBaseInventoryComponent[0];
			foreach (var buildState in EnumExtensions.GetValues<BuildingStates>())
			{
				IBaseInventoryComponent[] buildStateInventories;
				switch (buildState)
				{
					case BuildingStates.Unknown:
					case BuildingStates.Placing:
					case BuildingStates.Decaying:
						buildStateInventories = emptyInventories;
						break;
					case BuildingStates.Constructing:
						buildStateInventories = new IBaseInventoryComponent[] { ConstructionInventory };
						break;
					case BuildingStates.Operating:
						buildStateInventories = new IBaseInventoryComponent[] { Inventory };
						break;
					case BuildingStates.Salvaging:
						buildStateInventories = new IBaseInventoryComponent[] { SalvageInventory };
						break;
					default:
						Debug.LogError("Unrecognized BuildState: "+buildState);
						continue;
				}
				inventoriesByBuildingState.Add(buildState, buildStateInventories);
			}
			
			AppendComponents(
				Light,
				LightSensitive,
				Boundary,
				Health,
				Ownership,
				Inventory,
				ConstructionInventory,
				SalvageInventory,
				Obligations,
				Recipes,
				Farm,
				Activities,
				Tags,
				Enterable
			);
		}
	}
}