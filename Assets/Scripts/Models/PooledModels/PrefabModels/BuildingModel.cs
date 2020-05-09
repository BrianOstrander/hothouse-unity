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
		
		[JsonProperty] InventoryPermission inventoryPermission = Models.InventoryPermission.AllForAnyJob();
		[JsonIgnore] public readonly ListenerProperty<InventoryPermission> InventoryPermission;
		
		[JsonProperty] Inventory constructionRecipe = Models.Inventory.Empty;
		[JsonIgnore] public readonly ListenerProperty<Inventory> ConstructionRecipe;
		
		[JsonProperty] Inventory constructionRecipePromised = Models.Inventory.Empty;
		[JsonIgnore] public readonly ListenerProperty<Inventory> ConstructionRecipePromised;

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
			InventoryPermission = new ListenerProperty<InventoryPermission>(value => inventoryPermission = value, () => inventoryPermission);
			ConstructionRecipe = new ListenerProperty<Inventory>(value => constructionRecipe = value, () => constructionRecipe);
			ConstructionRecipePromised = new ListenerProperty<Inventory>(value => constructionRecipePromised = value, () => constructionRecipePromised);
			DesireQuality = new ListenerProperty<DesireQuality[]>(value => desireQuality = value, () => desireQuality);
			
			Entrances = new ListenerProperty<Entrance[]>(value => entrances = value, () => entrances);
		}
	}
}