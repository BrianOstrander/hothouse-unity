using System;
using System.Linq;
using Lunra.Core;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	// [BuildingDefinition]
	public abstract class FloraDefinition
	{
		public string Type { get; }
		protected virtual string[] PrefabIds { get; }

		protected virtual FloatRange AgeDuration => new FloatRange(30f, 60f);
		protected virtual FloatRange ReproductionDuration => new FloatRange(30f, 60f);
		protected virtual FloatRange ReproductionRadius => new FloatRange(0.5f, 1f);
		protected virtual int ReproductionFailureLimit => 40;
		protected virtual float HealthMaximum => 100f;
		protected virtual float SpreadDamage => 50f;
		protected virtual bool AttacksBuildings => false;
		protected virtual (Inventory.Types Type, int Minimum, int Maximum)[] ItemDrops => new (Inventory.Types Type, int Minimum, int Maximum)[0];
		protected virtual int CountPerRoomMinimum => 0;
		protected virtual int CountPerRoomMaximum => 4;
		protected virtual float SpawnDistanceNormalizedMinimum => 0f;
		protected virtual int CountPerClusterMinimum => 40;
		protected virtual int CountPerClusterMaximum => 60;
		protected virtual bool RequiredInSpawn => true;
		protected virtual bool AllowedInSpawn => true;

		// protected virtual Inventory GenerateDrops()
		// {
		// 	
		// }
		
		public FloraDefinition(string[] prefabIds)
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
			
		}
	}
}