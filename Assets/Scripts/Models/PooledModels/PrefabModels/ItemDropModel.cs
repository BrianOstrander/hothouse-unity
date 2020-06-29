using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public class ItemDropModel : PrefabModel,
		IInventoryModel
	{
		#region Serialized
		[JsonProperty] Jobs job;
		[JsonIgnore] public ListenerProperty<Jobs> Job { get; }
		
		public LightSensitiveComponent LightSensitive { get; } = new LightSensitiveComponent();
		public EnterableComponent Enterable { get; } = new EnterableComponent();
		public InventoryComponent Inventory { get; } = new InventoryComponent();
		#endregion
		
		#region Non Serialized
		public IBaseInventoryComponent[] Inventories { get; }
		#endregion

		public ItemDropModel()
		{
			Job = new ListenerProperty<Jobs>(value => job = value, () => job);
			Inventories = new []
			{
				Inventory	
			};
		}
	}
}