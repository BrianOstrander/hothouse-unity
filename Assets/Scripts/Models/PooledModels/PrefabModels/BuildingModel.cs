using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.Hothouse.Models.AgentModels;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public class BuildingModel : PrefabModel
	{
		#region Serialized
		[JsonProperty] BuildingStates buildingState;
		[JsonIgnore] public readonly ListenerProperty<BuildingStates> BuildingState;
		
		[JsonProperty] Inventory inventory = Models.Inventory.Empty;
		[JsonIgnore] public readonly ListenerProperty<Inventory> Inventory;
		
		[JsonProperty] InventoryCapacity inventoryCapacity = Models.InventoryCapacity.None();
		[JsonIgnore] public readonly ListenerProperty<InventoryCapacity> InventoryCapacity;
		
		[JsonProperty] InventoryPermission inventoryPermission = Models.InventoryPermission.AllForAnyJob();
		[JsonIgnore] public readonly ListenerProperty<InventoryPermission> InventoryPermission;
		
		[JsonProperty] Inventory constructionInventory = Models.Inventory.Empty;
		[JsonIgnore] public readonly ListenerProperty<Inventory> ConstructionInventory;
		
		[JsonProperty] InventoryCapacity constructionInventoryCapacity = Models.InventoryCapacity.None();
		[JsonIgnore] public readonly ListenerProperty<InventoryCapacity> ConstructionInventoryCapacity;

		[JsonProperty] Inventory constructionInventoryPromised = Models.Inventory.Empty;
		[JsonIgnore] public readonly ListenerProperty<Inventory> ConstructionInventoryPromised;
		
		[JsonProperty] Inventory salvageInventory = Models.Inventory.Empty;
		[JsonIgnore] public readonly ListenerProperty<Inventory> SalvageInventory;

		[JsonProperty] DesireQuality[] desireQuality = new DesireQuality[0];
		[JsonIgnore] public readonly ListenerProperty<DesireQuality[]> DesireQuality;
		#endregion
		
		#region Non Serialized
		Entrance[] entrances = new Entrance[0];
		public readonly ListenerProperty<Entrance[]> Entrances;
		#endregion

		public Action<DwellerModel, Desires> Operate = ActionExtensions.GetEmpty<DwellerModel, Desires>();
		
		public BuildingModel()
		{
			BuildingState = new ListenerProperty<BuildingStates>(value => buildingState = value, () => buildingState);
			Inventory = new ListenerProperty<Inventory>(value => inventory = value, () => inventory);
			InventoryCapacity = new ListenerProperty<InventoryCapacity>(value => inventoryCapacity = value, () => inventoryCapacity);
			InventoryPermission = new ListenerProperty<InventoryPermission>(value => inventoryPermission = value, () => inventoryPermission);
			ConstructionInventory = new ListenerProperty<Inventory>(value => constructionInventory = value, () => constructionInventory);
			ConstructionInventoryCapacity = new ListenerProperty<InventoryCapacity>(value => constructionInventoryCapacity = value, () => constructionInventoryCapacity);
			ConstructionInventoryPromised = new ListenerProperty<Inventory>(value => constructionInventoryPromised = value, () => constructionInventoryPromised);
			SalvageInventory = new ListenerProperty<Inventory>(value => salvageInventory = value, () => salvageInventory);
			DesireQuality = new ListenerProperty<DesireQuality[]>(value => desireQuality = value, () => desireQuality);
			
			Entrances = new ListenerProperty<Entrance[]>(value => entrances = value, () => entrances);
		}
	}
}