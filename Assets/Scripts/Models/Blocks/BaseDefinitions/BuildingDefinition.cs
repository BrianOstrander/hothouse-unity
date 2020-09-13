using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.Satchel;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	[BuildingDefinition]
	public abstract class BuildingDefinition : BaseDefinition<BuildingModel>
	{
		public override string DefaultPrefabId => "debug";

		public virtual float HealthMaximum => 100f;
		public virtual FloatRange PlacementLightRequirement => new FloatRange(0.001f, 1f);
		// public virtual Inventory LightFuel => Inventory.Empty;
		public virtual Interval LightFuelInterval => Interval.Zero();
		public virtual LightStates LightState => LightStates.Unknown;
		public virtual int MaximumOwners => 0;
		public virtual InventoryPermission DefaultInventoryPermission => InventoryPermission.NoneForAnyJob();

		public virtual int GetCapacities(List<(int Count, PropertyFilter Filter)> capacities) => 0;
		// public virtual InventoryCapacity DefaultInventoryCapacity => InventoryCapacity.None();
		// public virtual InventoryDesire DefaultInventoryDesire => InventoryDesire.UnCalculated(Inventory.Empty);
		// public virtual Inventory DefaultInventory => Inventory.Empty;
		// public virtual Inventory ConstructionInventory => Inventory.FromEntry(Inventory.Types.Stalk, 2);
		// public virtual Inventory SalvageInventory => ConstructionInventory * 0.5f;
		public virtual Recipe[] Recipes => new Recipe[0];
		public virtual bool IsFarm => false;
		public virtual Vector2 FarmSize => Vector2.zero;
		public virtual Type FarmFloraType => null;
		public virtual GoalActivity[] Activities => new GoalActivity[0];
		public virtual Jobs[] WorkplaceForJobs => new Jobs[0];
		public virtual string[] Tags => new string[0];
		public virtual Type[] BuildingsRequired => new Type[0];

		public virtual void Reset(
			BuildingModel model,
			BuildingStates state
		)
		{
			model.Type.Value = Type;

			model.PlacementLightRequirement.Value = PlacementLightRequirement;
			model.BuildingState.Value = state;
			
			model.Health.ResetToMaximum(HealthMaximum);
			// model.Light.Reset(
			// 	LightFuel,
			// 	LightFuelInterval,
			// 	LightState
			// );
			Debug.LogWarning("TODO: Light reset");
			
			model.LightSensitive.Reset();
			
			model.Ownership.Reset(
				MaximumOwners,
				WorkplaceForJobs
			);
			
			/*
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
				InventoryPermission.DepositForJobs(Jobs.Stockpiler, Jobs.Laborer), 
				InventoryCapacity.ByIndividualWeight(ConstructionInventory),
				constructionInventoryDesired
			);
			
			model.SalvageInventory.Reset(
				InventoryPermission.WithdrawalForJobs(Jobs.Stockpiler),
				InventoryCapacity.ByIndividualWeight(SalvageInventory),
				salvageInventoryDesired
			);

			model.SalvageInventory.Add(SalvageInventory);
			*/
			//Debug.LogWarning("TODO: Inventory, all of it lol");

			var capacities = new List<(int Count, PropertyFilter Filter)>();
			var capacityPool = GetCapacities(capacities);
			
			if (capacities.Any())
			{
				model.Inventory.Container.New(
					1,
					out var capacityPoolItem,
					Items.Instantiate.CapacityPool
						.Of(capacityPool)
				);
				
				foreach (var capacity in capacities)
				{
					var filterId = Game.Items.IdCounter.Next();
					model.Inventory.Capacities.Add(
						filterId,
						capacity.Filter
					);

					model.Inventory.Container.New(
						1,
						Items.Instantiate.Capacity
							.Of(
								filterId,
								capacityPoolItem.Id,
								capacity.Count
							)
					);
				}
			}
			else if (0 < capacityPool) Debug.LogError($"Specified resource capacity of {capacityPool} but no filters provided");
			
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

			var farmFloraType = Game.Flora.Definitions.FirstOrDefault(d => d.GetType() == FarmFloraType)?.Type;
			
			if (IsFarm && string.IsNullOrEmpty(farmFloraType)) Debug.LogError($"{Type} is a farm, but no seed of type {FarmFloraType} could be found");

			model.Farm.Reset(
				IsFarm,
				FarmSize,
				farmFloraType
			);	

			model.Activities.Reset(Activities);
			
			model.Tags.Reset();
			foreach (var tag in Tags) model.Tags.AddTag(tag); 
		}

		public override void Instantiate(BuildingModel model) => new BuildingPresenter(Game, model);

		#region Utility
		protected string GetActionName(
			Motives motive,
			string suffix = null
		)
		{
			var result = GetActionName(motive.ToString().ToLower());
			return string.IsNullOrEmpty(suffix) ? result : result + "_" + suffix;
		}

		protected string GetActionName(string suffix) => Type + "." + suffix;

		/*
		protected GoalActivity GetDefaultEatActivity(
			Inventory.Types type,
			float comfort = 0.1f,
			float durationInMinutes = 15f
		)
		{
			if (!DefaultEatModifiers.TryGetValue(type, out var result))
			{
				Debug.LogError("Unable to find eat modifier entry of type: " + type);
				return default;
			}
			
			return new GoalActivity(
				GetActionName(Motives.Eat, type.ToString().ToLower()),
				new[]
				{
					(Motives.Eat, result),
					(Motives.Comfort, comfort)
				},
				DayTime.FromMinutes(durationInMinutes),
				Inventory.FromEntry(type, 1)
			);
		}
		
		protected static readonly Dictionary<Inventory.Types, float> DefaultEatModifiers = new Dictionary<Inventory.Types, float>
		{
			{ Inventory.Types.Berries, -0.25f }
		};
		*/
		#endregion
	}
}