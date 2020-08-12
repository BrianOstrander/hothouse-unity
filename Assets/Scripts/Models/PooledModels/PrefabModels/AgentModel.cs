using Lunra.Hothouse.Ai;
using Newtonsoft.Json;
using Lunra.StyxMvp.Models;

namespace Lunra.Hothouse.Models
{
	public abstract class AgentModel : PrefabModel,
		IHealthModel,
		IAgentInventoryModel,
		IInventoryPromiseModel,
		IObligationPromiseModel,
		ITagModel
	{
		#region Serialized
		[JsonProperty] float navigationVelocity;
		[JsonIgnore] public ListenerProperty<float> NavigationVelocity { get; }
		
		[JsonProperty] float navigationForceDistanceMaximum;
		[JsonIgnore] public ListenerProperty<float> NavigationForceDistanceMaximum { get; }
		
		[JsonProperty] NavigationPlan navigationPlan = Models.NavigationPlan.Done();
		[JsonIgnore] public ListenerProperty<NavigationPlan> NavigationPlan { get; }
		
		[JsonProperty] float reachRadius;
		[JsonIgnore] public ListenerProperty<float> InteractionRadius { get; }

		[JsonProperty] public HealthComponent Health { get; private set; } = new HealthComponent();
		[JsonProperty] public AgentInventoryComponent Inventory { get; private set; } = new AgentInventoryComponent();
		[JsonProperty] public InventoryPromiseComponent InventoryPromises { get; private set; } = new InventoryPromiseComponent();
		[JsonProperty] public ObligationPromiseComponent ObligationPromises { get; private set; } = new ObligationPromiseComponent();
		[JsonProperty] public TagComponent Tags { get; private set; } = new TagComponent();

		[JsonProperty] public bool IsDebugging { get; set; }
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public AgentContext Context { get; set; }
		[JsonIgnore] public IBaseInventoryComponent[] Inventories { get; }
		[JsonIgnore] public IAgentStateMachine StateMachine { get; set; }
		#endregion
		
		public AgentModel()
		{
			NavigationVelocity = new ListenerProperty<float>(value => navigationVelocity = value, () => navigationVelocity);
			NavigationForceDistanceMaximum = new ListenerProperty<float>(value => navigationForceDistanceMaximum = value, () => navigationForceDistanceMaximum);
			NavigationPlan = new ListenerProperty<NavigationPlan>(value => navigationPlan = value, () => navigationPlan);
			InteractionRadius = new ListenerProperty<float>(value => reachRadius = value, () => reachRadius);
			
			Inventories = new []
			{
				Inventory	
			};
			
			AppendComponents(
				Health,
				Inventory,
				InventoryPromises,
				ObligationPromises,
				Tags
			);
		}
	}
}