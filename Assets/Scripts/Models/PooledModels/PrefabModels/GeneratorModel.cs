using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class GeneratorModel : PrefabModel,
		IGeneratorModel
	{
		#region Serialized
		[JsonProperty] InstanceId parent = InstanceId.Null();
		[JsonIgnore] public ListenerProperty<InstanceId> Parent { get; }
		
		[JsonProperty] public LightSensitiveComponent LightSensitive { get; private set; } = new LightSensitiveComponent();
		[JsonProperty] public EnterableComponent Enterable { get; private set; } = new EnterableComponent();
		[JsonProperty] public InventoryComponent Inventory { get; private set; } = new InventoryComponent();
		[JsonProperty] public GeneratorComponent Generator { get; private set; } = new GeneratorComponent();
		#endregion

		#region Non Serialized
		[JsonIgnore] public IBaseInventoryComponent[] Inventories { get; }
		#endregion

		public GeneratorModel()
		{
			Parent = new ListenerProperty<InstanceId>(value => parent = value, () => parent);
			
			Inventories = new [] { Inventory };
			
			AppendComponents(
				LightSensitive,
				Enterable,
				Inventory,
				Generator
			);
		}
	}
}