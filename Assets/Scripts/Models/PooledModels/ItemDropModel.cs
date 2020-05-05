using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.WildVacuum.Models
{
	public class ItemDropModel : PooledModel
	{
		#region Serialized
		[JsonProperty] Inventory inventory = Models.Inventory.Empty;
		[JsonIgnore] public readonly ListenerProperty<Inventory> Inventory;
		#endregion

		public ItemDropModel()
		{
			Inventory = new ListenerProperty<Inventory>(value => inventory = value, () => inventory);
		}
	}
}