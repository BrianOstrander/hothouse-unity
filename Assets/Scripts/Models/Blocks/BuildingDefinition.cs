using System;
using System.Linq;
using Lunra.Core;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	[BuildingDefinition]
	public abstract class BuildingDefinition
	{
		public string Type { get; }
		public virtual string PrefabId => Type;

		protected virtual float HealthMaximum => 100f;
		protected virtual FloatRange PlacementLightRequirement => new FloatRange(0.001f, 1f);
		protected virtual Inventory LightFuel => Inventory.Empty;
		protected virtual Interval LightFuelInterval => Interval.Zero();
		protected virtual LightStates LightState => LightStates.Unknown;
		// protected virtual LightStates LightState => LightStates.Fueled; // <- what light sources should override
		protected virtual DesireQuality[] DesireQualities => new DesireQuality[0];
		protected virtual int MaximumOwners => 0;
		protected virtual InventoryPermission DefaultInventoryPermission => InventoryPermission.NoneForAnyJob();
		protected virtual InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.None();
		protected virtual InventoryDesire DefaultInventoryDesire => InventoryDesire.Ignored();
		protected virtual Inventory DefaultInventory => Inventory.Empty;
		protected virtual Inventory ConstructionInventory => Inventory.FromEntry(Inventory.Types.StalkDry, 2);
		protected virtual Inventory SalvageInventory => ConstructionInventory * 0.5f;
		
		public BuildingDefinition()
		{
			Type = string.Concat(
				GetType().Name
					.Replace("Definition", String.Empty)
					.Select((c, i) => 0 < i && char.IsUpper(c) ? "_" + c : c.ToString())
			).ToLower();
		}
		
		public virtual void Reset(
			BuildingModel model,
			BuildingStates state
		)
		{
			model.Type.Value = Type;

			model.PlacementLightRequirement.Value = PlacementLightRequirement;
			model.DesireQualities.Value = DesireQualities;
			model.BuildingState.Value = state;
			
			model.Health.ResetToMaximum(HealthMaximum);
			model.Light.Reset(
				LightFuel,
				LightFuelInterval,
				LightState
			);
			model.LightSensitive.Reset();
			
			model.Ownership.Reset(MaximumOwners);
			
			model.Inventory.Reset(
				DefaultInventoryPermission,
				DefaultInventoryCapacity,
				DefaultInventoryDesire
			);

			model.Inventory.Add(DefaultInventory);

			var constructionInventoryDesired = InventoryDesire.Ignored();
			var salvageInventoryDesired = InventoryDesire.Ignored();

			switch (state)
			{
				case BuildingStates.Placing:
				case BuildingStates.Constructing:
					constructionInventoryDesired = InventoryDesire.UnCalculated(ConstructionInventory);
					break;
				case BuildingStates.Salvaging:
					salvageInventoryDesired = InventoryDesire.UnCalculated(SalvageInventory);
					break;
				case BuildingStates.Operating:
				case BuildingStates.Decaying:
					break;
				default:
					Debug.LogError("Unrecognized BuildingState: " + state);
					break;
			}
			
			model.ConstructionInventory.Reset(
				InventoryPermission.DepositForJobs(Jobs.Stockpiler), 
				InventoryCapacity.ByIndividualWeight(ConstructionInventory),
				constructionInventoryDesired
			);
			
			model.SalvageInventory.Reset(
				InventoryPermission.WithdrawalForJobs(Jobs.Stockpiler),
				InventoryCapacity.ByIndividualWeight(SalvageInventory),
				salvageInventoryDesired
			);

			model.SalvageInventory.Add(SalvageInventory);
			
			model.Obligations.Reset();
		}
	}
}