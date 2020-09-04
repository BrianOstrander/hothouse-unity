using System.Collections.Generic;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IInventoryPromiseModel : IInventoryModel, IHealthModel
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

		protected override void OnInitialize()
		{
			Debug.Log("Initializing...");
		}

		public override void Bind()
		{
			Debug.Log("Binding...");
			Model.Health.Destroyed += OnHealthDestroyed;
		}
		
		public override void UnBind()
		{
			Model.Health.Destroyed -= OnHealthDestroyed;
		}
		
		#region HealthComponent Events
		void OnHealthDestroyed(Damage.Result result)
		{
			Debug.LogError("TODO: Inventory promise breaking here");
		}
		#endregion
		
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