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
		
		[JsonProperty] int promisedClearerCount;
		[JsonIgnore] public ListenerProperty<int> PromisedClearerCount { get; }
		
		[JsonProperty] int promisedClearerMaximum;
		[JsonIgnore] public ListenerProperty<int> PromisedClearerMaximum { get; }
		
		[JsonProperty] bool promisedClearersAtCapacity;
		[JsonIgnore] public ListenerProperty<bool> PromisedClearersAtCapacity { get; }

		[JsonProperty] int? clearancePriority;
		[JsonIgnore] public ListenerProperty<int?> ClearancePriority { get; }
		
		[JsonProperty] SelectionStates selectionState = SelectionStates.NotSelected;
		[JsonIgnore] public ListenerProperty<SelectionStates> SelectionState { get; }
		#endregion
		
		#region NonSerialized
		bool isMarkedForClearance;
		[JsonIgnore] public DerivedProperty<bool, int?> IsMarkedForClearance { get; }
		#endregion
		
		public ClearableComponent()
		{
			ItemDrops = new ListenerProperty<Inventory>(value => itemDrops = value, () => itemDrops);
			MeleeRangeBonus = new ListenerProperty<float>(value => meleeRangeBonus = value, () => meleeRangeBonus);
			PromisedClearerCount = new ListenerProperty<int>(value => promisedClearerCount = value, () => promisedClearerCount);
			PromisedClearerMaximum = new ListenerProperty<int>(value => promisedClearerMaximum = value, () => promisedClearerMaximum);
			PromisedClearersAtCapacity = new ListenerProperty<bool>(value => promisedClearersAtCapacity = value, () => promisedClearersAtCapacity);
			ClearancePriority = new ListenerProperty<int?>(value => clearancePriority = value, () => clearancePriority);
			SelectionState = new ListenerProperty<SelectionStates>(value => selectionState = value, () => selectionState);
			
			IsMarkedForClearance = new DerivedProperty<bool, int?>(
				value => isMarkedForClearance = value,
				() => isMarkedForClearance,
				value => value.HasValue,
				ClearancePriority
			);
		}

		public void Reset()
		{
			SelectionState.Value = SelectionStates.NotSelected;
			ClearancePriority.Value = null;
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