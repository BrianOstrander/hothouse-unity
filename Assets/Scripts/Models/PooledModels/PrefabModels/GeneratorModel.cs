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
		
		public LightSensitiveComponent LightSensitive { get; } = new LightSensitiveComponent();
		public EnterableComponent Enterable { get; } = new EnterableComponent();
		public InventoryComponent Inventory { get; } = new InventoryComponent();
		public GeneratorComponent Generator { get; } = new GeneratorComponent();
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