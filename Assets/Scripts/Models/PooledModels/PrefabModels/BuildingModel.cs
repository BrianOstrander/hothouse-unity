using System;
using Lunra.Core;
using Lunra.Hothouse.Models.AgentModels;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public class BuildingModel : PrefabModel
	{
		#region Serialized
		[JsonProperty] Inventory inventory = Models.Inventory.Empty;
		[JsonIgnore] public readonly ListenerProperty<Inventory> Inventory;

		[JsonProperty] DesireQuality[] desireQuality = new DesireQuality[0];
		[JsonIgnore] public readonly ListenerProperty<DesireQuality[]> DesireQuality;
		#endregion
		
		#region Non Serialized
		Entrance[] entrances = new Entrance[0];
		public readonly ListenerProperty<Entrance[]> Entrances;
		#endregion

		public Action<DwellerModel, Desires> Operate = ActionExtensions.GetEmpty<DwellerModel, Desires>();
		
		public BuildingModel()
		{
			Entrances = new ListenerProperty<Entrance[]>(value => entrances = value, () => entrances);
			Inventory = new ListenerProperty<Inventory>(value => inventory = value, () => inventory);
			DesireQuality = new ListenerProperty<DesireQuality[]>(value => desireQuality = value, () => desireQuality);
		}
	}
}