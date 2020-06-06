using Lunra.Core;
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
		[SerializeField] Light light;
		[SerializeField] FloatRange lightIntensityRange;
		[SerializeField] AnimationCurve lightIntensityCurve;
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

		public Vector3 CameraForward { set => messageLabel.transform.forward = value; }

		public float LightLevel
		{
			set
			{
				var intensity = lightIntensityRange.Evaluate(lightIntensityCurve.Evaluate(value));
				light.intensity = intensity;
				light.enabled = !Mathf.Approximately(0f, intensity);
			}
		}
		#endregion
  
		public override void Reset()
		{
			base.Reset();

			UpdateValidation(
				BuildValidationModel.ValidationStates.None,
				string.Empty
			);
			
			CameraForward = Vector3.forward;

			LightLevel = 0f;
		}
  
		#region Events
		#endregion
	}
   
}