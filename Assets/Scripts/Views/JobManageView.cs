using System;
using Lunra.Core;
using Lunra.StyxMvp;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lunra.Hothouse.Views
{
	public class JobManageView : View
	{
		public struct ControlClickResult
		{
			public bool IsIncreaseEnabled;
			public bool IsDecreaseEnabled;
			public int Count;
		}
		
		public struct ControlEntry
		{
			public string Name;
			public Func<ControlClickResult> GetCount;
			public Func<int, ControlClickResult> ModifyCount;
		}
		
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] JobManageControlLeaf controlPrefab;
		[SerializeField] GameObject controlsRoot;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public void SetControls(params ControlEntry[] controls)
		{
			controlsRoot.transform.ClearChildren();
			foreach (var control in controls)
			{
				var instance = controlsRoot.InstantiateChild(
					controlPrefab,
					setActive: true
				);

				instance.Control = control;
			}
		}
		#endregion
		
		public override void Cleanup()
		{
			base.Cleanup();

			SetControls();
		}

		#region Events
		#endregion
	}
 
}