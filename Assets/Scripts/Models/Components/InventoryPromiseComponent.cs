using System.Collections.Generic;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public interface IInventoryPromiseModel : IRoomTransformModel
	{
		InventoryPromiseComponent InventoryPromises { get; }
	}

	public class InventoryPromiseComponent : Model
	{
		#region Serialized
		[JsonProperty] Stack<InventoryTransaction> transactions = new Stack<InventoryTransaction>();
		[JsonIgnore] public StackProperty<InventoryTransaction> Transactions { get; }
		#endregion
		
		#region Non Serialized
		#endregion

		public InventoryPromiseComponent()
		{
			Transactions = new StackProperty<InventoryTransaction>(transactions);
		}

		public void Reset()
		{
			Transactions.Clear();
		}
	}
}