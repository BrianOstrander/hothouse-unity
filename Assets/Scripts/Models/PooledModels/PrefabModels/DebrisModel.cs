namespace Lunra.Hothouse.Models
{
	public class DebrisModel : PrefabModel, IClearableModel
	{
		#region Serialized
		public LightSensitiveComponent LightSensitive { get; } = new LightSensitiveComponent();
		public HealthComponent Health { get; } = new HealthComponent();
		public ClearableComponent Clearable { get; } = new ClearableComponent();
		public ObligationComponent Obligations { get; } = new ObligationComponent();
		public EnterableComponent Enterable { get; } = new EnterableComponent();
		#endregion
		
		#region NonSerialized
		#endregion
		
		public DebrisModel() { }
	}
}