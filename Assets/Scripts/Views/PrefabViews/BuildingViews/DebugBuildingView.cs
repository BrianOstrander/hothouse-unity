using TMPro;
using UnityEngine;

namespace Lunra.WildVacuum.Views
{
	public class DebugBuildingView : BuildingView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] TextMeshPro label;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		public string Text
		{
			set => label.text = value;
		}
		
		public override void Reset()
		{
			base.Reset();

			Text = string.Empty;
		}

	}
}