using Newtonsoft.Json;
namespace Lunra.Hothouse.Models
{
	public class DebrisModel : PrefabModel, IClearableModel
	{
		#region Serialized
		[JsonProperty] public LightSensitiveComponent LightSensitive { get; private set; } = new LightSensitiveComponent();
		[JsonProperty] public HealthComponent Health { get; private set; } = new HealthComponent();
		[JsonProperty] public ClearableComponent Clearable { get; private set; } = new ClearableComponent();
		[JsonProperty] public ObligationComponent Obligations { get; private set; } = new ObligationComponent();
		[JsonProperty] public EnterableComponent Enterable { get; private set; } = new EnterableComponent();
		#endregion
		
		#region NonSerialized
		#endregion

		public DebrisModel()
		{
			AppendComponents(
				LightSensitive,
				Health,
				Clearable,
				Obligations,
				Enterable
			);
		}
	}
}