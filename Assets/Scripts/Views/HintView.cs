using Lunra.StyxMvp;
using TMPro;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class HintView : View
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] TextMeshProUGUI messageLabel;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public string Message { set => messageLabel.text = value ?? string.Empty; }
		#endregion

		public override void Cleanup()
		{
			base.Cleanup();

			Message = string.Empty;
		}

		#region Events
		#endregion
	}
 
}