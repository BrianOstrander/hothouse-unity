using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class InstanceId
	{
		public static InstanceId New<T>(T model)
			where T : class, IModel
		{
			return new InstanceId(model);
		}
		
		public static InstanceId New(Types type, string id) => new InstanceId(type, id, null);
		
		public static InstanceId Null() => new InstanceId(Types.Null, null, null);

		public static Types GetTypeFromInstance<T>(T model)
			where T : class, IModel
		{
			switch (model)
			{
				case null:
					return Types.Null;
				case DwellerModel _:
					return Types.Dweller;
				case FloraModel _:
					return Types.Flora;
				case BuildingModel _:
					return Types.Building;
				case RoomModel _:
					return Types.Room;
				case DoorModel _:
					return Types.Door;
				case SeekerModel _:
					return Types.Seeker;
				default:
					Debug.LogError("Unrecognized model type: "+model.GetType());
					return Types.Unknown;
			}
		}
		
		public enum Types
		{
			Unknown = 0,
			Null = 10,
			Dweller = 20,
			Flora = 30,
			Building = 40,
			Room = 50,
			Door = 60,
			Seeker = 70
		}

		public Types Type { get; }
		public string Id { get; }

		IModel cachedInstance;

		[JsonIgnore] public bool IsNull => Type == Types.Null;
		
		InstanceId(IModel instance) : this(
			GetTypeFromInstance(instance),
			instance.Id.Value,
			instance
		) { }
		
		InstanceId(
			Types type,
			string id,
			IModel instance
		)
		{
			Type = type;
			Id = id;
			cachedInstance = instance;
		}

		public bool TryGetInstance<T>(GameModel game, out T instance)
			where T : class, IModel
		{
			instance = default;
			
			switch (Type)
			{
				case Types.Unknown:
					Debug.LogError("Trying to get the instance of unsupported type: " + Type);
					return false;
				case Types.Null:
					return false;
			}

			if (string.IsNullOrEmpty(Id))
			{
				Debug.LogError("Trying to get supported type " + Type + " but the Id is null or empty");
				return false;
			}

			if (cachedInstance != null)
			{
				if (cachedInstance.Id.Value == Id)
				{
					instance = cachedInstance as T;
					
					if (instance != null) return true;
					
					cachedInstance = null;
				}
			}

			switch (Type)
			{
				case Types.Dweller:
					cachedInstance = game.Dwellers.FirstOrDefaultActive(Id);
					break;
				case Types.Flora:
					cachedInstance = game.Flora.FirstOrDefaultActive(Id);
					break;
				case Types.Building:
					cachedInstance = game.Buildings.FirstOrDefaultActive(Id);
					break;
				case Types.Room:
					cachedInstance = game.Rooms.FirstOrDefaultActive(Id);
					break;
				case Types.Door:
					cachedInstance = game.Doors.FirstOrDefaultActive(Id);
					break;
				case Types.Seeker:
					cachedInstance = game.Seekers.FirstOrDefaultActive(Id);
					break;
				default:
					Debug.LogError("Unrecognized type: " + Type);
					return false;
			}

			instance = cachedInstance as T;

			if (instance == null) Debug.LogError("Successfully found a "+Type+" but was unable to convert it to "+typeof(T));
			
			return instance != null;
		}
	}

	public static class InstanceIdExtensions
	{
		public static InstanceId GetInstanceId(this IModel model) => InstanceId.New(model);
	}
}