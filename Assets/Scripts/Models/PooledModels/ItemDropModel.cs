using Newtonsoft.Json;
using Lunra.StyxMvp.Models;
using Lunra.Hothouse.Models.AgentModels;

namespace Lunra.Hothouse.Models
{
	public class ItemDropModel : PooledModel
	{
		#region Serialized
		[JsonProperty] Inventory inventory = Models.Inventory.Empty;
		[JsonIgnore] public readonly ListenerProperty<Inventory> Inventory;

		[JsonProperty] Jobs job;
		[JsonIgnore] public readonly ListenerProperty<Jobs> Job;
		#endregion

		public ItemDropModel()
		{
			Inventory = new ListenerProperty<Inventory>(value => inventory = value, () => inventory);
			Job = new ListenerProperty<Jobs>(value => job = value, () => job);
		}
	}
}