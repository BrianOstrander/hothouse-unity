using System;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	[BuildingDefinition]
	public abstract class BuildingDefinition : BaseDefinition<BuildingModel>
	{
		public override string DefaultPrefabId => "debug";

		public virtual float HealthMaximum => 100f;
		public virtual FloatRange PlacementLightRequirement => new FloatRange(0.001f, 1f);
		public virtual Inventory LightFuel => Inventory.Empty;
		public virtual Interval LightFuelInterval => Interval.Zero();
		public virtual LightStates LightState => LightStates.Unknown;
		public virtual int MaximumOwners => 0;
		public virtual InventoryPermission DefaultInventoryPermission => InventoryPermission.NoneForAnyJob();
		public virtual InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.None();
		public virtual InventoryDesire DefaultInventoryDesire => InventoryDesire.Ignored();
		public virtual Inventory DefaultInventory => Inventory.Empty;
		public virtual Inventory ConstructionInventory => Inventory.FromEntry(Inventory.Types.StalkDry, 2);
		public virtual Inventory SalvageInventory => ConstructionInventory * 0.5f;
		public virtual Recipe[] Recipes => new Recipe[0];
		public virtual bool IsFarm => false;
		public virtual Vector2 FarmSize => Vector2.zero;
		public virtual Inventory.Types FarmSeed => Inventory.Types.Unknown;
		
		public virtual void Reset(
			BuildingModel model,
			BuildingStates state
		)
		{
			model.Type.Value = Type;

			model.PlacementLightRequirement.Value = PlacementLightRequirement;
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
			
			model.Recipes.Reset(Recipes);

			/*
			if (Recipes.Any())
			{
				model.Recipes.Queue.Value = new[]
				{
					RecipeComponent.RecipeIteration.ForCount(Recipes.First(), 10),
					RecipeComponent.RecipeIteration.ForDesired(Recipes.First(), 5)
				};
			}
			*/
			
			model.Farm.Reset(
				IsFarm,
				FarmSize,
				FarmSeed
			);
		}

		public override void Instantiate(BuildingModel model) => new BuildingPresenter(Game, model);
	}
}