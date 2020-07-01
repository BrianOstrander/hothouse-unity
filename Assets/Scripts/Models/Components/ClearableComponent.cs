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

	public class ClearableComponent : Model
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

	public static class ClearableGameModelExtensions
	{
		public static IEnumerable<IClearableModel> GetClearables(
			this GameModel game	
		)
		{
			return game.Debris.AllActive
				.Concat<IClearableModel>(game.Flora.AllActive);
		}
	}
}