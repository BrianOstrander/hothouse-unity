using System.Collections.Generic;
using Lunra.Satchel;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IInventoryPromiseModel : IInventoryModel
	{
		InventoryPromiseComponent InventoryPromises { get; }
	}
	
	public class InventoryPromiseComponent : ComponentModel<IInventoryPromiseModel>
	{
		#region Serialized
		[JsonProperty] List<long> all = new List<long>();
		[JsonIgnore] public StackProperty<long> All { get; } 
		#endregion
		
		#region Non Serialized
		#endregion

		public InventoryPromiseComponent()
		{
			All = new StackProperty<long>(all);
		}
		
		public void Reset()
		{
			All.Clear();
			ResetId();
		}

		public override string ToString()
		{
			var result = "Inventory Promise Component [ " + ShortId + " ]:\n";
			return result;
		}
	}
}