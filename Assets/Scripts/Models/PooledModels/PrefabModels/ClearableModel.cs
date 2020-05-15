using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public interface IClearableModel : IPrefabModel
	{
		#region Serialized
		ListenerProperty<float> Health { get; }
		ListenerProperty<float> HealthMaximum { get; }
		ListenerProperty<Inventory> ItemDrops { get; }
		ListenerProperty<float> MeleeRangeBonus { get; }
		ListenerProperty<int> PromisedClearerCount { get; }
		ListenerProperty<int> PromisedClearerMaximum { get; }
		ListenerProperty<bool> PromisedClearersAtCapacity { get; }
		ListenerProperty<int?> ClearancePriority { get; }
		ListenerProperty<SelectionStates> SelectionState { get; }
		#endregion
		
		#region NonSerialized
		DerivedProperty<bool, int?> IsMarkedForClearance { get; }
		#endregion
	}
	
	public class ClearableModel : PrefabModel, IClearableModel
	{
		#region Serialized
		[JsonProperty] float health = -1f;
		[JsonIgnore] public ListenerProperty<float> Health { get; }
		
		[JsonProperty] float healthMaximum;
		[JsonIgnore] public ListenerProperty<float> HealthMaximum { get; }
		
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
		
		[JsonProperty] SelectionStates selectionState = SelectionStates.Deselected;
		[JsonIgnore] public ListenerProperty<SelectionStates> SelectionState { get; }
		#endregion
		
		#region NonSerialized
		bool isMarkedForClearance;
		[JsonIgnore] public DerivedProperty<bool, int?> IsMarkedForClearance { get; }
		#endregion
		
		public ClearableModel()
		{
			Health = new ListenerProperty<float>(value => health = value, () => health);
			HealthMaximum = new ListenerProperty<float>(value => healthMaximum = value, () => healthMaximum);
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
	}
}