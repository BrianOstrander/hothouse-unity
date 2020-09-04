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
		IRecipeModel,
		IFarmModel,
		IGoalActivityModel,
		ILightSensitiveModel
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
		[JsonProperty] public ObligationComponent Obligations { get; private set; } = new ObligationComponent();
		[JsonProperty] public RecipeComponent Recipes { get; private set; } = new RecipeComponent();
		[JsonProperty] public FarmComponent Farm { get; private set; } = new FarmComponent();
		[JsonProperty] public GoalActivityComponent Activities { get; private set; } = new GoalActivityComponent();
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public EnterableComponent Enterable { get; } = new EnterableComponent();
		#endregion

		public bool IsBuildingState(BuildingStates buildingState) => BuildingState.Value == buildingState;
		
		public BuildingModel()
		{
			Type = new ListenerProperty<string>(value => type = value, () => type);
			BuildingState = new ListenerProperty<BuildingStates>(value => buildingState = value, () => buildingState);
			PlacementLightRequirement = new ListenerProperty<FloatRange>(value => placementLightRequirement = value, () => placementLightRequirement);
		
			AppendComponents(
				Light,
				LightSensitive,
				Boundary,
				Health,
				Ownership,
				Inventory,
				Obligations,
				Recipes,
				Farm,
				Activities,
				Enterable
			);
		}
	}
}