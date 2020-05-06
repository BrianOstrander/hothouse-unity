using UnityEngine;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.WildVacuum.Models
{
	public abstract class BuildingModel : PrefabModel
	{
		public struct Entrance
		{
			public enum States
			{
				Unknown = 0,
				Available = 10,
				Blocked = 20
			}
			
			public readonly Vector3 Position;
			public readonly States State;

			public Entrance(
				Vector3 position,
				States state
			)
			{
				Position = position;
				State = state;
			}
		}
		
		#region Serialized
		#endregion
		
		#region Non Serialized
		Entrance[] entrances = new Entrance[0];
		public readonly ListenerProperty<Entrance[]> Entrances;
		#endregion
		
		public BuildingModel()
		{
			Entrances = new ListenerProperty<Entrance[]>(value => entrances = value, () => entrances);
		}
	}
}