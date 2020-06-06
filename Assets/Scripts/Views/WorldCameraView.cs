using System;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class WorldCameraView : View
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] Camera cameraInstance;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Reverse Bindings
		public Camera CameraInstance => cameraInstance;
		#endregion

		protected override void OnPrepare()
		{
			base.OnPrepare();
			
			AlignCamera();
		}

		/*
		void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(CameraInstance.transform.position, RootTransform.position);
		}
		*/

		#region Utility
		[ContextMenu("Align Camera")]
		void AlignCamera()
		{
			CameraInstance.transform.LookAt(RootTransform.position);
		}
		#endregion
	}

}