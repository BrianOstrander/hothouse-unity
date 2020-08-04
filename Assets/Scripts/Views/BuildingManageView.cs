using System;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp;
using TMPro;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class BuildingManageView : View
	{
		public struct Control
		{
			public enum Types
			{
				Unknown = 0,
				Label = 10,
				Button = 20,
				RadioButtonEnabled = 30,
				RadioButtonDisabled = 40
			}

			public Types Type;
			public string LabelText;
			public string ButtonText;
			public Action Click;
		}
	
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] Transform controlRoot;
		[SerializeField] BuildingManageControlLeaf[] prefabs;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public void Controls(params Control[] controls)
		{
			controlRoot.ClearChildren();

			foreach (var control in controls)
			{
				var prefab = prefabs.FirstOrDefault(p => p.Type == control.Type);

				if (prefab == null)
				{
					Debug.LogError("No prefab for Type: "+control.Type);
					continue;
				}

				var instance = controlRoot.gameObject.InstantiateChild(prefab, setActive: true);

				switch (control.Type)
				{
					case Control.Types.Label:
						instance.Label.text = control.LabelText;
						break;
					case Control.Types.Button:
						instance.ButtonLabel.text = control.ButtonText;
						instance.Button.onClick.AddListener(() => control.Click?.Invoke());
						break;
					case Control.Types.RadioButtonDisabled:
						instance.Label.text = control.LabelText;
						instance.Button.onClick.AddListener(() => control.Click?.Invoke());
						break;
					case Control.Types.RadioButtonEnabled:
						instance.Label.text = control.LabelText;
						instance.Button.onClick.AddListener(() => control.Click?.Invoke());
						break;
					default:
						Debug.LogError("Unrecognized Type: "+control.Type);
						break;
				}
			}
		}
		#endregion

		public override void Cleanup()
		{
			base.Cleanup();

			foreach (var prefab in prefabs) prefab.gameObject.SetActive(false);

			Controls();
		}

		#region Events
		#endregion
	}
 
}