using System;
using Lunra.Core;
using Lunra.StyxMvp;
using TMPro;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class GameResultView : View
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		
		[SerializeField] TextMeshProUGUI descriptionLabel;
		[SerializeField] TextMeshProUGUI buttonLabel;
		
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public string Description
		{
			set => descriptionLabel.text = value;
		}
		
		public string ButtonDescription
		{
			set => buttonLabel.text = value;
		}
		
		public event Action Click;
		#endregion

		public override void Reset()
		{
			base.Reset();

			Description = string.Empty;
			ButtonDescription = string.Empty;
			Click = null;
		}

		#region Events
		public void OnClick() => Click?.Invoke();
		#endregion
	}

}