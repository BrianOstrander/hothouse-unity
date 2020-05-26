using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Models.AgentModels;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public class BuildingModel : PrefabModel, ILightModel, ILightSensitiveModel
	{
		#region Serialized
		[JsonProperty] Buildings type;
		[JsonIgnore] public ListenerProperty<Buildings> Type { get; }
		
		[JsonProperty] BuildingStates buildingState;
		[JsonIgnore] public ListenerProperty<BuildingStates> BuildingState { get; }
		
		[JsonProperty] Inventory inventory = Models.Inventory.Empty;
		[JsonIgnore] public ListenerProperty<Inventory> Inventory { get; }
		
		[JsonProperty] InventoryCapacity inventoryCapacity = Models.InventoryCapacity.None();
		[JsonIgnore] public ListenerProperty<InventoryCapacity> InventoryCapacity { get; }
		
		[JsonProperty] InventoryPermission inventoryPermission = Models.InventoryPermission.AllForAnyJob();
		[JsonIgnore] public ListenerProperty<InventoryPermission> InventoryPermission { get; }

		[JsonProperty] FloatRange placementLightRequirement = FloatRange.Zero;
		[JsonIgnore] public ListenerProperty<FloatRange> PlacementLightRequirement { get; }
		
		[JsonProperty] Inventory constructionInventory = Models.Inventory.Empty;
		[JsonIgnore] public ListenerProperty<Inventory> ConstructionInventory { get; }
		
		[JsonProperty] InventoryCapacity constructionInventoryCapacity = Models.InventoryCapacity.None();
		[JsonIgnore] public ListenerProperty<InventoryCapacity> ConstructionInventoryCapacity { get; }

		[JsonProperty] Inventory constructionInventoryPromised = Models.Inventory.Empty;
		[JsonIgnore] public ListenerProperty<Inventory> ConstructionInventoryPromised { get; }
		
		[JsonProperty] Inventory salvageInventory = Models.Inventory.Empty;
		[JsonIgnore] public ListenerProperty<Inventory> SalvageInventory { get; }

		[JsonProperty] DesireQuality[] desireQualities = new DesireQuality[0];
		[JsonIgnore] public ListenerProperty<DesireQuality[]> DesireQualities { get; }

		[JsonProperty] bool isLight;
		[JsonIgnore] public ListenerProperty<bool> IsLight { get; }
		[JsonProperty] LightStates lightState;
		[JsonIgnore] public ListenerProperty<LightStates> LightState { get; }
		[JsonProperty] Inventory lightFuel;
		[JsonIgnore] public ListenerProperty<Inventory> LightFuel { get; }
		[JsonProperty] Interval lightFuelInterval;
		[JsonIgnore] public ListenerProperty<Interval> LightFuelInterval { get; }
		[JsonProperty] bool isLightRefueling;
		[JsonIgnore] public ListenerProperty<bool> IsLightRefueling { get; }
		
		[JsonProperty] float lightLevel;
		[JsonIgnore] public ListenerProperty<float> LightLevel { get; }
		#endregion
		
		#region Non Serialized
		Entrance[] entrances = new Entrance[0];
		public ListenerProperty<Entrance[]> Entrances { get; }
		
		[JsonProperty] float lightRadius;
		[JsonIgnore] public ListenerProperty<float> LightRange { get; }
		
		[JsonIgnore] public Action<DwellerModel, Desires> Operate = ActionExtensions.GetEmpty<DwellerModel, Desires>();
		#endregion

		public bool IsBuildingState(BuildingStates buildingState) => BuildingState.Value == buildingState;
		public bool IsDesireAvailable(Desires desire) => DesireQualities.Value.Any(d => d.Desire == desire && d.State == DesireQuality.States.Available);
		
		public BuildingModel()
		{
			Type = new ListenerProperty<Buildings>(value => type = value, () => type);
			BuildingState = new ListenerProperty<BuildingStates>(value => buildingState = value, () => buildingState);
			Inventory = new ListenerProperty<Inventory>(value => inventory = value, () => inventory);
			InventoryCapacity = new ListenerProperty<InventoryCapacity>(value => inventoryCapacity = value, () => inventoryCapacity);
			InventoryPermission = new ListenerProperty<InventoryPermission>(value => inventoryPermission = value, () => inventoryPermission);
			PlacementLightRequirement = new ListenerProperty<FloatRange>(value => placementLightRequirement = value, () => placementLightRequirement);
			ConstructionInventory = new ListenerProperty<Inventory>(value => constructionInventory = value, () => constructionInventory);
			ConstructionInventoryCapacity = new ListenerProperty<InventoryCapacity>(value => constructionInventoryCapacity = value, () => constructionInventoryCapacity);
			ConstructionInventoryPromised = new ListenerProperty<Inventory>(value => constructionInventoryPromised = value, () => constructionInventoryPromised);
			SalvageInventory = new ListenerProperty<Inventory>(value => salvageInventory = value, () => salvageInventory);
			DesireQualities = new ListenerProperty<DesireQuality[]>(value => desireQualities = value, () => desireQualities);
			IsLight = new ListenerProperty<bool>(value => isLight = value, () => isLight);
			LightState = new ListenerProperty<LightStates>(value => lightState = value, () => lightState);
			LightFuel = new ListenerProperty<Inventory>(value => lightFuel = value, () => lightFuel);
			LightFuelInterval = new ListenerProperty<Interval>(value => lightFuelInterval = value, () => lightFuelInterval);
			IsLightRefueling = new ListenerProperty<bool>(value => isLightRefueling = value, () => isLightRefueling);
			LightLevel = new ListenerProperty<float>(value => lightLevel = value, () => lightLevel);
			
			Entrances = new ListenerProperty<Entrance[]>(value => entrances = value, () => entrances);
			LightRange = new ListenerProperty<float>(value => lightRadius = value, () => lightRadius);
		}
	}
}