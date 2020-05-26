using Lunra.Hothouse.Models;
using Lunra.StyxMvp;
using TMPro;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class BuildValidationView : View
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] GameObject invalidWidget;
		[SerializeField] GameObject validWidget;
		[SerializeField] TextMeshPro messageLabel;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
  
		#region Bindings
		public void UpdateValidation(
			BuildValidationModel.ValidationStates validation,
			string message
		)
		{
			invalidWidget.SetActive(validation == BuildValidationModel.ValidationStates.Invalid);
			validWidget.SetActive(validation == BuildValidationModel.ValidationStates.Valid);
			
			messageLabel.text = message ?? string.Empty;
		}

		public Vector3 CameraPosition { set => messageLabel.transform.LookAt(-value); }
		#endregion
  
		public override void Reset()
		{
			base.Reset();

			UpdateValidation(
				BuildValidationModel.ValidationStates.None,
				string.Empty
			);
			
			CameraPosition = Vector3.zero;
		}
  
		#region Events
		#endregion
	}
   
}