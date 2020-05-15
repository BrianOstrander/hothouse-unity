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
		[JsonIgnore] public ListenerProperty<BuildingStates> BuildingState { get; }
		
		[JsonProperty] Inventory inventory = Models.Inventory.Empty;
		[JsonIgnore] public ListenerProperty<Inventory> Inventory { get; }
		
		[JsonProperty] InventoryCapacity inventoryCapacity = Models.InventoryCapacity.None();
		[JsonIgnore] public ListenerProperty<InventoryCapacity> InventoryCapacity { get; }
		
		[JsonProperty] InventoryPermission inventoryPermission = Models.InventoryPermission.AllForAnyJob();
		[JsonIgnore] public ListenerProperty<InventoryPermission> InventoryPermission { get; }
		
		[JsonProperty] Inventory constructionInventory = Models.Inventory.Empty;
		[JsonIgnore] public ListenerProperty<Inventory> ConstructionInventory { get; }
		
		[JsonProperty] InventoryCapacity constructionInventoryCapacity = Models.InventoryCapacity.None();
		[JsonIgnore] public ListenerProperty<InventoryCapacity> ConstructionInventoryCapacity { get; }

		[JsonProperty] Inventory constructionInventoryPromised = Models.Inventory.Empty;
		[JsonIgnore] public ListenerProperty<Inventory> ConstructionInventoryPromised { get; }
		
		[JsonProperty] Inventory salvageInventory = Models.Inventory.Empty;
		[JsonIgnore] public ListenerProperty<Inventory> SalvageInventory { get; }

		[JsonProperty] DesireQuality[] desireQuality = new DesireQuality[0];
		[JsonIgnore] public ListenerProperty<DesireQuality[]> DesireQuality { get; }
		#endregion
		
		#region Non Serialized
		Entrance[] entrances = new Entrance[0];
		public ListenerProperty<Entrance[]> Entrances { get; }
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