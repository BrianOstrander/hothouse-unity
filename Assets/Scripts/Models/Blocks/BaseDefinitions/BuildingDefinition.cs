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

		public virtual void GetCapacities(CapacityPoolBuilder capacityPoolBuilder) { }
		public virtual Recipe[] Recipes => new Recipe[0];
		public virtual bool IsFarm => false;
		public virtual Vector2 FarmSize => Vector2.zero;
		public virtual Type FarmFloraType => null;
		public virtual GoalActivity[] Activities => new GoalActivity[0];
		public virtual Jobs[] WorkplaceForJobs => new Jobs[0];
		public virtual string[] Tags => new string[0];
		public virtual Type[] BuildingsRequired => new Type[0];

		CapacityPoolBuilder capacityPoolBuilder;

		public override void Initialize(GameModel game, string[] prefabIds)
		{
			base.Initialize(game, prefabIds);
			
			capacityPoolBuilder = new CapacityPoolBuilder(Game);
		}

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

			try
			{
				GetCapacities(capacityPoolBuilder);
				capacityPoolBuilder.Apply(
					model.Inventory,
					capacityPoolType =>
					{
						// Confusing, but basically return true if this inventory should be forbidden.
						switch (state)
						{
							case BuildingStates.Placing:
								return true;
							case BuildingStates.Constructing:
								return capacityPoolType != Items.Values.CapacityPool.Types.Construction;
							case BuildingStates.Operating:
								return capacityPoolType != Items.Values.CapacityPool.Types.Stockpile;
							case BuildingStates.Salvaging:
								return capacityPoolType != Items.Values.CapacityPool.Types.Salvage;
							default:
								Debug.LogError($"Unrecognized state: {state}");
								return true;
						}
					}
				);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				capacityPoolBuilder.Reset();
			}

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