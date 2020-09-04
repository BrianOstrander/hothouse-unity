using System.Collections.Generic;
using Lunra.Satchel;
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
			var destroyed = new List<Stack>();
			
			foreach (var (item, stack) in Model.Inventory.Container.All())
			{
				var type = item[Items.Keys.Shared.Type];

				if (type == Items.Values.Shared.Types.Reservation)
				{
					destroyed.Add(stack);
					
					// if (item[Items.Keys.Reservation.])
					//
					
				}
				else if (type == Items.Values.Shared.Types.Transfer)
				{
					destroyed.Add(stack);

					var state = item[Items.Keys.Transfer.LogisticState];
					
					// if (state == Items.Values.Shared.LogisticStates.Dropoff)

				}
			}

			Model.Inventory.Container.Destroy(destroyed.ToArray());
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