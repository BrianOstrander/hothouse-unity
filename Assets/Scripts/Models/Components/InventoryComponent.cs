using System;
using System.Linq;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IInventoryModel : IRoomTransformModel
	{
		InventoryComponent Inventory { get; }
	}
	
	public class InventoryComponent : ComponentModel<IInventoryModel>
	{
		#region Serialized
		[JsonProperty] InventoryPermission permission = InventoryPermission.AllForAnyJob();
		[JsonProperty] public ListenerProperty<InventoryPermission> Permission { get; private set; }
		
		// [JsonProperty] Inventory all = Inventory.Empty;
		// protected readonly ListenerProperty<Inventory> AllListener;
		// [JsonIgnore] public ReadonlyProperty<Inventory> All { get; }
		#endregion
		
		#region Non Serialized
		#endregion

		public InventoryComponent()
		{
			Permission = new ListenerProperty<InventoryPermission>(value => permission = value, () => permission);
			Debug.LogError("TODO: Set up All");
		}

		public void Reset(
			InventoryPermission permission
		)
		{
			ResetId();
			
			Permission.Value = permission;
			Debug.LogError("TODO: More reset logic");
		}

		public override string ToString()
		{
			var result = "Inventory Component [ " + ShortId + " ]:";
			return result;
		}

		#region Utility
		void Recalculate()
		{
			
		}
		#endregion
	}
}