using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Satchel;

namespace Lunra.Hothouse.Models
{
	public class CapacityPoolBuilder
	{
		struct CapacityDefinition
		{
			public PropertyFilter Filter;
			public int Count;
		}
		
		struct CapacityPoolDefinition
		{
			public string Type;
			public int Count;
			public CapacityDefinition[] Capacities;
		}
		
		GameModel game;
		List<CapacityPoolDefinition> capacityPoolDefinitions = new List<CapacityPoolDefinition>();

		public CapacityPoolBuilder(GameModel game) => this.game = game;

		public CapacityPoolBuilder Pool(
			string type,
			int count,
			params (PropertyFilter Filter, int Count)[] capacities
		)
		{
			this.capacityPoolDefinitions.Add(
				new CapacityPoolDefinition
				{
					Type = type,
					Count = count,
					Capacities = capacities
						.Select(
							e => new CapacityDefinition
							{
								Filter = e.Filter,
								Count = e.Count
							}
						)
						.ToArray()
				}
			);

			return this;
		}
		
		public CapacityPoolBuilder Pool(
			string type,
			int count,
			params (string ResourceType, int Count)[] capacities
		)
		{
			return Pool(
				type,
				count,
				capacities
					.Select(
						e => (
							game.Items.Builder
								.BeginPropertyFilter()
								.RequireAll(PropertyValidations.String.EqualTo(Items.Keys.Resource.Type, e.ResourceType))
								.Done(),
							e.Count
						)
					)
					.ToArray()
			);
		}

		public CapacityPoolBuilder Pool(
			string type,
			int count,
			params string[] resourceTypes
		)
		{
			return Pool(
				type,
				count,
				resourceTypes
					.Select(e => (e, count))
					.ToArray()
			);
		}

		public void Apply(
			InventoryComponent inventory,
			Func<string, bool> forbiddenPredicate
		)
		{
			foreach (var capacityPoolDefinition in capacityPoolDefinitions)
			{
				inventory.Container.New(
					1,
					out var capacityPool,
					Items.Instantiate.CapacityPool
						.Of(
							capacityPoolDefinition.Type,	
							capacityPoolDefinition.Count,
							(Items.Keys.CapacityPool.IsForbidden, forbiddenPredicate(capacityPoolDefinition.Type))
						)
				);

				foreach (var capacityDefinition in capacityPoolDefinition.Capacities)
				{
					inventory.Container.New(
						1,
						out var capacity,
						Items.Instantiate.Capacity
							.Of(
								capacityPool.Id,
								capacityDefinition.Count
							)
					);
					
					inventory.Capacities.Add(
						capacity.Id,
						capacityDefinition.Filter
					);
				}
			}
			
			Reset();
		}

		public void Reset() => capacityPoolDefinitions.Clear();
	}
}