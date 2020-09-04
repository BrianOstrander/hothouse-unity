using Lunra.StyxMvp.Models;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class SnapCapModel : AgentModel, IClearableModel, IAttackModel, ILightSensitiveModel
	{
		#region Serialized
		[JsonProperty] DayTime huntForbiddenExpiration;
		[JsonIgnore] public ListenerProperty<DayTime> HuntForbiddenExpiration { get; }
		[JsonProperty] float huntRangeMaximum;
		[JsonIgnore] public ListenerProperty<float> HuntRangeMaximum { get; }
		[JsonProperty] DayTimeFrame awakeTime;
		[JsonIgnore] public ListenerProperty<DayTimeFrame> AwakeTime { get; }
		[JsonProperty] float navigationPathMaximum;
		[JsonIgnore] public ListenerProperty<float> NavigationPathMaximum { get; }
		
		[JsonProperty] public AttackComponent Attacks { get; private set; } = new AttackComponent();
		[JsonProperty] public LightSensitiveComponent LightSensitive { get; private set; } = new LightSensitiveComponent();
		[JsonProperty] public ClearableComponent Clearable { get; private set; } = new ClearableComponent();
		[JsonProperty] public ObligationComponent Obligations { get; private set; } = new ObligationComponent();
		[JsonProperty] public EnterableComponent Enterable { get; private set; } = new EnterableComponent();
		#endregion
		
		#region Non Serialized
		#endregion

		public SnapCapModel()
		{
			HuntForbiddenExpiration = new ListenerProperty<DayTime>(value => huntForbiddenExpiration = value, () => huntForbiddenExpiration);
			HuntRangeMaximum = new ListenerProperty<float>(value => huntRangeMaximum = value, () => huntRangeMaximum);
			AwakeTime = new ListenerProperty<DayTimeFrame>(value => awakeTime = value, () => awakeTime);
			NavigationPathMaximum = new ListenerProperty<float>(value => navigationPathMaximum = value, () => navigationPathMaximum);
			
			AppendComponents(
				Attacks,
				LightSensitive,
				Clearable,
				Obligations,
				Enterable
			);
		}
	}
}