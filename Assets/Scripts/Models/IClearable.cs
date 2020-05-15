using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public interface IClearable
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
		#endregion
		
		#region NonSerialized
		DerivedProperty<bool, int?> IsMarkedForClearance { get; }
		#endregion
	}
}