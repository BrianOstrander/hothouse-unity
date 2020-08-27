using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class ItemDropModel : PrefabModel,
		IInventoryModel,
		IEnterableModel
	{
		#region Serialized
		[JsonProperty] public LightSensitiveComponent LightSensitive { get; private set; } = new LightSensitiveComponent();
		[JsonProperty] public EnterableComponent Enterable { get; private set; } = new EnterableComponent();
		[JsonProperty] public InventoryComponent Inventory { get; private set; } = new InventoryComponent();
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public IBaseInventoryComponent[] Inventories { get; }
		#endregion

		public ItemDropModel()
		{
			Inventories = new [] { Inventory };
			
			AppendComponents(
				LightSensitive,
				Enterable,
				Inventory
			);
		}
	}
}