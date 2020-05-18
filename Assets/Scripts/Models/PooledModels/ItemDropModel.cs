using Newtonsoft.Json;
using Lunra.StyxMvp.Models;
using Lunra.Hothouse.Models.AgentModels;

namespace Lunra.Hothouse.Models
{
	public class ItemDropModel : PooledModel, ILightSensitiveModel
	{
		#region Serialized
		[JsonProperty] Inventory inventory = Models.Inventory.Empty;
		[JsonIgnore] public ListenerProperty<Inventory> Inventory { get; }
		
		[JsonProperty] Inventory withdrawalInventoryPromised = Models.Inventory.Empty;
		[JsonIgnore] public ListenerProperty<Inventory> WithdrawalInventoryPromised { get; }

		[JsonProperty] Jobs job;
		[JsonIgnore] public ListenerProperty<Jobs> Job { get; }
		
		[JsonProperty] float lightLevel;
		[JsonIgnore] public ListenerProperty<float> LightLevel { get; }
		#endregion

		public ItemDropModel()
		{
			Inventory = new ListenerProperty<Inventory>(value => inventory = value, () => inventory);
			WithdrawalInventoryPromised = new ListenerProperty<Inventory>(value => withdrawalInventoryPromised = value, () => withdrawalInventoryPromised);
			Job = new ListenerProperty<Jobs>(value => job = value, () => job);
			LightLevel = new ListenerProperty<float>(value => lightLevel = value, () => lightLevel);
		}
	}
}