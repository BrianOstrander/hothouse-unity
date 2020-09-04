using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class BuildingPoolModel : BasePrefabPoolModel<BuildingModel>
	{
		Dictionary<Type, BuildingDefinition> definitions = new Dictionary<Type, BuildingDefinition>();
		
		[JsonIgnore] public BuildingDefinition[] Definitions { get; private set; }
		[JsonIgnore] public ReadOnlyDictionary<Jobs, string[]> Workplaces { get; private set; }
		
		public override void Initialize(GameModel game)
		{
			var prefabIdsFromViews = App.V.GetPrefabs<BuildingView>()
				.Select(v => v.View.PrefabId)
				.ToArray();

			foreach (var definitionType in ReflectionUtility.GetTypesWithAttribute<BuildingDefinitionAttribute, BuildingDefinition>())
			{
				if (ReflectionUtility.TryGetInstanceOfType<BuildingDefinition>(definitionType, out var definitionInstance))
				{
					try
					{
						definitionInstance.Initialize(
							game,
							prefabIdsFromViews
						);
						definitions.Add(definitionType, definitionInstance);
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
				}
			}
			
			Definitions = definitions.Values.ToArray();

			var workplaces = new Dictionary<Jobs, string[]>();

			foreach (var job in EnumExtensions.GetValues(Jobs.Unknown))
			{
				workplaces.Add(
					job,
					Definitions
						.Where(d => d.WorkplaceForJobs.Contains(job))
						.Select(d => d.Type)
						.ToArray()
				);
			}

			Workplaces = new ReadOnlyDictionary<Jobs, string[]>(workplaces);
			
			Initialize(
				game,
				m => Definitions.First(d => d.Type == m.Type.Value).Instantiate(m)
			);
		}

		public BuildingModel Activate<T>(
			string roomId,
			Vector3 position,
			Quaternion rotation,
			BuildingStates buildingState
		)
			where T : BuildingDefinition
		{
			return Activate(
				definitions[typeof(T)],
				roomId,
				position,
				rotation,
				buildingState
			);
		}
		
		public BuildingModel Activate(
			BuildingDefinition definition,
			string roomId,
			Vector3 position,
			Quaternion rotation,
			BuildingStates buildingState
		)
		{
			var result = Activate(
				definition.GetPrefabId(),
				roomId,
				position,
				rotation,
				model => definition.Reset(model, buildingState)
			);
			// TODO: Probably need to move this to component initialize...
			if (IsInitialized) Game.LastLightUpdate.Value = Game.LastLightUpdate.Value.SetSensitiveStale(result.Id.Value);
			return result;
		}

		public string GetDefinitionType<T>()
			where T : BuildingDefinition
		{
			return GetDefinitionType(typeof(T));
		}
		
		public string GetDefinitionType(Type type)
		{
			return definitions[type].Type;
		}
	}
}