using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class ClearableModel_old : PrefabModel, IClearableModel
	{
		#region Serialized
		public LightSensitiveComponent LightSensitive { get; } = new LightSensitiveComponent();
		public HealthComponent Health { get; } = new HealthComponent();
		public ClearableComponent Clearable { get; } = new ClearableComponent();
		#endregion
		
		#region NonSerialized
		#endregion
		
		public ClearableModel_old()
		{
		}
	}
}