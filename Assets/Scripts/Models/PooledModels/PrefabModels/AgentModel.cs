using System;
using Lunra.Core;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public abstract class AgentModel : PrefabModel,
		IHealthModel,
		IAgentInventoryModel
	{
		#region Serialized
		[JsonProperty] float navigationVelocity;
		[JsonIgnore] public ListenerProperty<float> NavigationVelocity { get; }
		
		[JsonProperty] float navigationForceDistanceMaximum;
		[JsonIgnore] public ListenerProperty<float> NavigationForceDistanceMaximum { get; }
		
		[JsonProperty] NavigationPlan navigationPlan = Models.NavigationPlan.Done();
		[JsonIgnore] public ListenerProperty<NavigationPlan> NavigationPlan { get; }

		[JsonProperty] InventoryPromise inventoryPromise = Models.InventoryPromise.Default();
		[JsonIgnore] public ListenerProperty<InventoryPromise> InventoryPromise { get; }
		
		[JsonProperty] ObligationPromise obligation = ObligationPromise.Default();
		[JsonIgnore] public ListenerProperty<ObligationPromise> Obligation { get; }
		
		public HealthComponent Health { get; } = new HealthComponent();
		public AgentInventoryComponent Inventory { get; } = new AgentInventoryComponent();
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public bool IsDebugging { get; set; }
		[JsonIgnore] public AgentContext Context { get; set; }
		[JsonIgnore] public Action<Obligation> ObligationComplete { get; set; } = ActionExtensions.GetEmpty<Obligation>();
		#endregion
		
		public AgentModel()
		{
			NavigationVelocity = new ListenerProperty<float>(value => navigationVelocity = value, () => navigationVelocity);
			NavigationForceDistanceMaximum = new ListenerProperty<float>(value => navigationForceDistanceMaximum = value, () => navigationForceDistanceMaximum);
			NavigationPlan = new ListenerProperty<NavigationPlan>(value => navigationPlan = value, () => navigationPlan);
			InventoryPromise = new ListenerProperty<InventoryPromise>(value => inventoryPromise = value, () => inventoryPromise);
			Obligation = new ListenerProperty<ObligationPromise>(value => obligation = value, () => obligation);
		}
	}
}