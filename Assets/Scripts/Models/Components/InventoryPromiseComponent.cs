using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public interface IInventoryPromiseModel : IRoomTransformModel
	{
		AgentInventoryComponent Inventory { get; }
	}

	public class InventoryPromiseComponent : Model
	{
		#region Serialized
		#endregion
		
		#region Non Serialized
		#endregion

		public InventoryPromiseComponent()
		{
			
		}

		public void Promise(
			
		)
		{
			
		}
		
		public void Reset()
		{
			
		}
	}
}