namespace Lunra.Hothouse.Models
{
	public class ItemDropModel : PrefabModel,
		IInventoryModel
	{
		#region Serialized
		public LightSensitiveComponent LightSensitive { get; } = new LightSensitiveComponent();
		public EnterableComponent Enterable { get; } = new EnterableComponent();
		public InventoryComponent Inventory { get; } = new InventoryComponent();
		#endregion
		
		#region Non Serialized
		public IBaseInventoryComponent[] Inventories { get; }
		#endregion

		public ItemDropModel()
		{
			Inventories = new []
			{
				Inventory	
			};
		}
	}
}