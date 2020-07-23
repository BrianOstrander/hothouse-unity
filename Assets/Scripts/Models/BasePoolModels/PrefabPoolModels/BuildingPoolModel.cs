using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
using Lunra.Hothouse.Views;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class BuildingPoolModel : BasePrefabPoolModel<BuildingModel>
	{
		GameModel game;
		Dictionary<Type, BuildingDefinition> definitions = new Dictionary<Type, BuildingDefinition>();
		
		public BuildingDefinition[] Definitions { get; private set; }
		
		public override void Initialize(GameModel game)
		{
			this.game = game;
			
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
			
			Initialize(
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

			if (IsInitialized) game.LastLightUpdate.Value = game.LastLightUpdate.Value.SetSensitiveStale(result.Id.Value);
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