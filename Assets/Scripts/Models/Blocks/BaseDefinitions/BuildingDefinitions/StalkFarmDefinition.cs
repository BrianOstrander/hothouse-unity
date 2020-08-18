using System;
using System.Linq;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class StalkFarmDefinition : BuildingDefinition
	{
		public override string DefaultPrefabId => "debug_small";
		
		public override int MaximumOwners => 2;

		public override bool IsFarm => true;
		public override Vector2 FarmSize => Vector2.one * 8f;
		public override Type FarmFloraType => typeof(StalkDefinition);
		public override Jobs[] WorkplaceForJobs => new[] {Jobs.Farmer};
	}
}