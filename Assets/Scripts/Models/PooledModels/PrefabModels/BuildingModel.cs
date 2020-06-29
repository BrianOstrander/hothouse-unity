using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public class BuildingModel : PrefabModel,
		ILightModel,
		IBoundaryModel,
		IHealthModel,
		IClaimOwnershipModel,
		IConstructionModel,
		IInventoryModel
	{
		#region Serialized
		[JsonProperty] Buildings type;
		[JsonIgnore] public ListenerProperty<Buildings> Type { get; }
		
		[JsonProperty] BuildingStates buildingState;
		[JsonIgnore] public ListenerProperty<BuildingStates> BuildingState { get; }

		[JsonProperty] FloatRange placementLightRequirement = FloatRange.Zero;
		[JsonIgnore] public ListenerProperty<FloatRange> PlacementLightRequirement { get; }
		
		[JsonProperty] Inventory salvageInventory = Models.Inventory.Empty;
		[JsonIgnore] public ListenerProperty<Inventory> SalvageInventory { get; }

		[JsonProperty] DesireQuality[] desireQualities = new DesireQuality[0];
		[JsonIgnore] public ListenerProperty<DesireQuality[]> DesireQualities { get; }

		public LightComponent Light { get; } = new LightComponent();
		public LightSensitiveComponent LightSensitive { get; } = new LightSensitiveComponent();
		public BoundaryComponent Boundary { get; } = new BoundaryComponent();
		public HealthComponent Health { get; } = new HealthComponent();
		public ClaimComponent Ownership { get; } = new ClaimComponent();
		public InventoryComponent Inventory { get; } = new InventoryComponent();
		public InventoryComponent ConstructionInventory { get; } = new InventoryComponent();
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public EnterableComponent Enterable { get; } = new EnterableComponent();
		
		[JsonIgnore] public Action<DwellerModel, Desires> Operate = ActionExtensions.GetEmpty<DwellerModel, Desires>();
		[JsonIgnore] public IBaseInventoryComponent[] Inventories { get; }
		#endregion

		public bool IsBuildingState(BuildingStates buildingState) => BuildingState.Value == buildingState;
		public bool IsDesireAvailable(Desires desire) => DesireQualities.Value.Any(d => d.Desire == desire && d.State == DesireQuality.States.Available);
		
		public BuildingModel()
		{
			Type = new ListenerProperty<Buildings>(value => type = value, () => type);
			BuildingState = new ListenerProperty<BuildingStates>(value => buildingState = value, () => buildingState);
			PlacementLightRequirement = new ListenerProperty<FloatRange>(value => placementLightRequirement = value, () => placementLightRequirement);
			SalvageInventory = new ListenerProperty<Inventory>(value => salvageInventory = value, () => salvageInventory);
			DesireQualities = new ListenerProperty<DesireQuality[]>(value => desireQualities = value, () => desireQualities);
			Inventories = new []
			{
				Inventory,
				ConstructionInventory
			};
		}
	}
}