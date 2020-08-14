using System.Collections.Generic;
using System.Linq;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public interface IClearableModel : IPrefabModel, IHealthModel, IObligationModel
	{
		ClearableComponent Clearable { get; }
	}

	public class ClearableComponent : ComponentModel<IClearableModel>
	{
		#region Serialized
		[JsonProperty] Inventory itemDrops = Inventory.Empty;
		[JsonIgnore] public ListenerProperty<Inventory> ItemDrops { get; }
		
		[JsonProperty] float meleeRangeBonus;
		[JsonIgnore] public ListenerProperty<float> MeleeRangeBonus { get; }
		#endregion
		
		#region NonSerialized
		#endregion
		
		public ClearableComponent()
		{
			ItemDrops = new ListenerProperty<Inventory>(value => itemDrops = value, () => itemDrops);
			MeleeRangeBonus = new ListenerProperty<float>(value => meleeRangeBonus = value, () => meleeRangeBonus);
		}
	}
}