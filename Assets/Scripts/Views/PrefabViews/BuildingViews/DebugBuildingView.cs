using TMPro;
using UnityEngine;

namespace Lunra.Hothouse.Views
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
			set
			{
				label.text = value;
				label.gameObject.SetActive(!string.IsNullOrEmpty(value));
			}
		}

		public override void Cleanup()
		{
			base.Cleanup();

			Text = string.Empty;
		}

	}
}