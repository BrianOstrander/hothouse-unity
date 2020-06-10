using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class RoomPrefabView : PrefabView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] Light[] lights;
		[SerializeField] AnimationCurve lightIntensityByTimeOfDay;
		[SerializeField] GameObject unexploredRoot;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Bindings
		public float TimeOfDay
		{
			set
			{
				foreach (var light in lights) light.intensity = lightIntensityByTimeOfDay.Evaluate(value);
			}
		}

		public bool IsExplored { set => unexploredRoot.SetActive(!value); }
		
		public string RoomId { get; set; }
		#endregion

		public override void Reset()
		{
			base.Reset();

			TimeOfDay = 0f;
			IsExplored = false;
			RoomId = null;
		}
	}

}