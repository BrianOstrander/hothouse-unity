using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Presenters;
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

			foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()))
			{
				if (type.IsAbstract) continue;
				if (type.GetCustomAttributes(typeof(BuildingDefinitionAttribute), true).None()) continue;
				if (!typeof(BuildingDefinition).IsAssignableFrom(type))
				{
					Debug.LogError("The class \"" + type.FullName + "\" tries to include the \"" + typeof(BuildingDefinitionAttribute).Name + "\" attribute without inheriting from the \"" + typeof(BuildingDefinition).FullName + "\" class");
					continue;
				}

				try
				{
					var instance = (BuildingDefinition) Activator.CreateInstance(type);
					
					if (definitions.TryGetValue(type, out var existingInstance))
					{
						Debug.LogError("Tried to add building definition of type " + type.FullName + " but an entry of type " + existingInstance.GetType().FullName + " already exists");
					}
					else
					{
						definitions.Add(type, instance);
					}
				}
				catch (Exception e) { Debug.LogException(e); }
			}

			Definitions = definitions.Values.ToArray();
			// Debug.Log("found:\n"+definitions.Keys.ToReadableJson());
			
			Initialize(
				model => new BuildingPresenter(game, model)	
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
			BuildingDefinition buildingDefinition,
			string roomId,
			Vector3 position,
			Quaternion rotation,
			BuildingStates buildingState
		)
		{
			var result = Activate(
				buildingDefinition.PrefabId,
				roomId,
				position,
				rotation,
				model => buildingDefinition.Reset(model, buildingState)
			);

			if (IsInitialized) game.LastLightUpdate.Value = game.LastLightUpdate.Value.SetSensitiveStale(result.Id.Value);
			return result;
		}

		public string GetSerializedType<T>()
			where T : BuildingDefinition
		{
			return definitions[typeof(T)].Type;
		}
	}
}