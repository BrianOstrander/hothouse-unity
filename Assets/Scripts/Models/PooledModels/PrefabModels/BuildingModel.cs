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
		IBoundaryModel,
		IHealthModel,
		IClaimOwnershipModel,
		IConstructionModel,
		IRecipeModel,
		IFarmModel
	{
		#region Serialized
		[JsonProperty] string type;
		[JsonIgnore] public ListenerProperty<string> Type { get; }
		
		[JsonProperty] BuildingStates buildingState;
		[JsonIgnore] public ListenerProperty<BuildingStates> BuildingState { get; }

		[JsonProperty] FloatRange placementLightRequirement = FloatRange.Zero;
		[JsonIgnore] public ListenerProperty<FloatRange> PlacementLightRequirement { get; }
		
		[JsonProperty] DesireQuality[] desireQualities = new DesireQuality[0];
		[JsonIgnore] public ListenerProperty<DesireQuality[]> DesireQualities { get; }

		public LightComponent Light { get; } = new LightComponent();
		public LightSensitiveComponent LightSensitive { get; } = new LightSensitiveComponent();
		public BoundaryComponent Boundary { get; } = new BoundaryComponent();
		public HealthComponent Health { get; } = new HealthComponent();
		public ClaimComponent Ownership { get; } = new ClaimComponent();
		public InventoryComponent Inventory { get; } = new InventoryComponent();
		public InventoryComponent ConstructionInventory { get; } = new InventoryComponent();
		public InventoryComponent SalvageInventory { get; } = new InventoryComponent();
		public ObligationComponent Obligations { get; } = new ObligationComponent();
		public RecipeComponent Recipes { get; } = new RecipeComponent();
		public FarmComponent Farm { get; } = new FarmComponent();
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public EnterableComponent Enterable { get; } = new EnterableComponent();
		
		[JsonIgnore] public Action<DwellerModel, Desires> Operate = ActionExtensions.GetEmpty<DwellerModel, Desires>();
		
		Dictionary<BuildingStates, IBaseInventoryComponent[]> inventoriesByBuildingState = new Dictionary<BuildingStates, IBaseInventoryComponent[]>();
		[JsonIgnore] public IBaseInventoryComponent[] Inventories => inventoriesByBuildingState[BuildingState.Value];
		#endregion

		public bool IsBuildingState(BuildingStates buildingState) => BuildingState.Value == buildingState;
		public bool IsDesireAvailable(Desires desire) => DesireQualities.Value.Any(d => d.Desire == desire && d.State == DesireQuality.States.Available);
		
		public BuildingModel()
		{
			Type = new ListenerProperty<string>(value => type = value, () => type);
			BuildingState = new ListenerProperty<BuildingStates>(value => buildingState = value, () => buildingState);
			PlacementLightRequirement = new ListenerProperty<FloatRange>(value => placementLightRequirement = value, () => placementLightRequirement);
			DesireQualities = new ListenerProperty<DesireQuality[]>(value => desireQualities = value, () => desireQualities);

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
		}
	}
}