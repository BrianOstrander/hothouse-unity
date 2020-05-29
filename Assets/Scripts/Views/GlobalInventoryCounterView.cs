using Lunra.StyxMvp;
using TMPro;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class GlobalInventoryCounterView : View
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] TextMeshProUGUI label;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public string Label { set => label.text = value ?? string.Empty; }
		#endregion

		public override void Reset()
		{
			base.Reset();

			Label = string.Empty;
		}

		#region Events
		#endregion
	}
 
}