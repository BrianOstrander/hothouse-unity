using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IInventoryPromiseModel : IRoomTransformModel
	{
		InventoryPromiseComponent InventoryPromises { get; }
	}

	public class InventoryPromiseComponent : ComponentModel<IInventoryPromiseModel>
	{
		#region Serialized
		[JsonProperty] List<InventoryTransaction> transactions = new List<InventoryTransaction>();
		[JsonIgnore] public StackProperty<InventoryTransaction> Transactions { get; }
		#endregion
		
		#region Non Serialized
		#endregion

		public InventoryPromiseComponent()
		{
			Transactions = new StackProperty<InventoryTransaction>(transactions);
		}

		public void Push(
			Inventory inventory,
			InventoryComponent source,
			InventoryComponent destination
		)
		{
			if (source.Id.Value == destination.Id.Value) throw new Exception("Cannot transfer between the same inventory!");
			
			Transactions.Push(
				destination.RequestDeliver(inventory)	
			);
			
			Transactions.Push(
				source.RequestDistribution(inventory)	
			);
		}

		public void BreakAllPromises()
		{
			foreach (var transaction in Transactions.PeekAll())
			{
				if (!transaction.Target.TryGetInstance<IBaseInventoryComponent>(Game, out var target)) continue;

				switch (target)
				{
					case InventoryComponent inventory:
						switch (transaction.Type)
						{
							case InventoryTransaction.Types.Deliver:
								inventory.CompleteDeliver(transaction, false);
								break;
							case InventoryTransaction.Types.Distribute:
								inventory.CompleteDistribution(transaction, false);
								break;
							default:
								Debug.LogError("Unrecognized transaction type: "+transaction.Type);
								break;
						}
						break;
					case AgentInventoryComponent _:
						break;
					default:
						Debug.LogError("Unrecognized transaction target type: "+target.GetType());
						break;
				}
			}
			// Debug.LogWarning("Handle unfulfilled inventory promises here");	
		}

		public void Reset()
		{
			Transactions.Clear();
		}

		public override string ToString()
		{
			var result = "Promise Transactions:";
			var all = Transactions.PeekAll();
			
			if (all.None()) return result + " None";

			foreach (var transaction in all)
			{
				result += "\n - " + transaction;
			}

			return result;
		}
	}
}