using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.Hothouse.Views;
using Lunra.NumberDemon;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class FloraPoolModel : BasePrefabPoolModel<FloraModel>
	{
		GameModel game;
		Dictionary<Type, FloraDefinition> definitions = new Dictionary<Type, FloraDefinition>();
		Demon generator = new Demon();
		
		public FloraDefinition[] Definitions { get; private set; }
		
		public override void Initialize(GameModel game)
		{
			this.game = game;

			var prefabIdsFromViews = App.V.GetPrefabs<FloraView>()
				.Select(v => v.View.PrefabId)
				.ToArray();

			foreach (var definitionType in ReflectionUtility.GetTypesWithAttribute<FloraDefinitionAttribute, FloraDefinition>())
			{
				if (ReflectionUtility.TryGetInstanceOfType<FloraDefinition>(definitionType, out var definitionInstance))
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

		public FloraModel Activate<T>(
			string roomId,
			Vector3 position,
			Quaternion? rotation = null,
			bool isAdult = false,
			Demon generator = null
		)
			where T : FloraDefinition
		{
			return Activate(
				definitions[typeof(T)],
				roomId,
				position,
				rotation,
				isAdult,
				generator
			);
		}
		
		public FloraModel Activate(
			string type,
			string roomId,
			Vector3 position,
			Quaternion? rotation = null,
			bool isAdult = false,
			Demon generator = null
		)
		{
			return Activate(
				Definitions.First(d => d.Type == type),
				roomId,
				position,
				rotation,
				isAdult,
				generator
			);
		}
		
		public FloraModel Activate(
			FloraDefinition definition,
			string roomId,
			Vector3 position,
			Quaternion? rotation = null,
			bool isAdult = false,
			Demon generator = null
		)
		{
			var result = Activate(
				definition.GetPrefabId(generator),
				roomId,
				position,
				rotation ?? (generator ?? this.generator).GetNextRotation(),
				model =>
				{
					definition.Reset(model);
					if (isAdult) model.Age.Value = model.Age.Value.Done();
				}
			);
			if (IsInitialized) game.LastLightUpdate.Value = game.LastLightUpdate.Value.SetSensitiveStale(result.Id.Value);
			return result;
		}
		
		public FloraDefinition[] GetTypesValidForRoom(RoomModel room)
		{
			if (room.IsSpawn.Value)
			{
				return Definitions
					.Where(d => d.RequiredInSpawn || d.AllowedInSpawn)
					.ToArray();
			}

			return Definitions
				.Where(d => d.SpawnDistanceNormalizedMinimum <= room.SpawnDistanceNormalized.Value)
				.ToArray();
		}
		
		public string GetDefinitionType<T>()
			where T : FloraDefinition
		{
			return GetDefinitionType(typeof(T));
		}
		
		public string GetDefinitionType(Type type)
		{
			return definitions[type].Type;
		}
	}
}