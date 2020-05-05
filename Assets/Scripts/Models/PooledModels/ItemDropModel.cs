using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.WildVacuum.Models
{
	public class ItemDropModel : PooledModel
	{
		#region Serialized
		[JsonProperty] Inventory itemDrops = Inventory.Empty;
		[JsonIgnore] public readonly ListenerProperty<Inventory> ItemDrops;
		#endregion

		public ItemDropModel()
		{
			ItemDrops = new ListenerProperty<Inventory>(value => itemDrops = value, () => itemDrops);
		}
	}
}