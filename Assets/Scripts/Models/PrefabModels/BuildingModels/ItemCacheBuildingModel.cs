using UnityEngine;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.WildVacuum.Models
{
	public class ItemCacheBuildingModel : BuildingModel
	{
		#region Serialized
		[JsonProperty] Inventory inventory = Models.Inventory.Empty;
		[JsonIgnore] public readonly ListenerProperty<Inventory> Inventory;
		#endregion
		
		#region Non Serialized
		#endregion
		
		public ItemCacheBuildingModel()
		{
			Inventory = new ListenerProperty<Inventory>(value => inventory = value, () => inventory);
		}
	}
}