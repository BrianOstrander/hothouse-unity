using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public abstract class AgentModel : PrefabModel
	{
		#region Serialized
		[JsonProperty] float navigationVelocity;
		[JsonIgnore] public ListenerProperty<float> NavigationVelocity { get; }
		
		[JsonProperty] float navigationForceDistanceMaximum;
		[JsonIgnore] public ListenerProperty<float> NavigationForceDistanceMaximum { get; }
		
		[JsonProperty] NavigationPlan navigationPlan = Models.NavigationPlan.Done();
		[JsonIgnore] public ListenerProperty<NavigationPlan> NavigationPlan { get; }

		[JsonProperty] float health;
		[JsonIgnore] public ListenerProperty<float> Health { get; }
		
		[JsonProperty] float healthMaximum;
		[JsonIgnore] public ListenerProperty<float> HealthMaximum { get; }
		
		[JsonProperty] Inventory inventory = Models.Inventory.Empty;
		[JsonIgnore] public ListenerProperty<Inventory> Inventory { get; }

		[JsonProperty] InventoryCapacity inventoryCapacity = Models.InventoryCapacity.None();
		[JsonIgnore] public ListenerProperty<InventoryCapacity> InventoryCapacity { get; }

		[JsonProperty] InventoryPromise inventoryPromise = Models.InventoryPromise.Default();
		[JsonIgnore] public ListenerProperty<InventoryPromise> InventoryPromise { get; }
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public bool IsDebugging { get; set; }
		[JsonIgnore] public AgentContext Context { get; set; } 
		#endregion
		
		public AgentModel()
		{
			NavigationVelocity = new ListenerProperty<float>(value => navigationVelocity = value, () => navigationVelocity);
			NavigationForceDistanceMaximum = new ListenerProperty<float>(value => navigationForceDistanceMaximum = value, () => navigationForceDistanceMaximum);
			NavigationPlan = new ListenerProperty<NavigationPlan>(value => navigationPlan = value, () => navigationPlan);
			Health = new ListenerProperty<float>(value => health = value, () => health);
			HealthMaximum = new ListenerProperty<float>(value => healthMaximum = value, () => healthMaximum);
			Inventory = new ListenerProperty<Inventory>(value => inventory = value, () => inventory);
			InventoryCapacity = new ListenerProperty<InventoryCapacity>(value => inventoryCapacity = value, () => inventoryCapacity);
			InventoryPromise = new ListenerProperty<InventoryPromise>(value => inventoryPromise = value, () => inventoryPromise);
		}
	}
}