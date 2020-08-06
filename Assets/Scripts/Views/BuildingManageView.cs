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
				RadioButtonDisabled = 40,
				LeftRightButton = 50
			}

			public Control(
				Types type,
				string[] labelText = null,
				string[] buttonText = null,
				Action[] click = null
			)
			{
				Type = type;
				LabelText = labelText ?? new string[0];
				ButtonText = buttonText ?? new string[0];
				Click = click ?? new Action[0];
			}

			public Types Type;
			public string[] LabelText;
			public string[] ButtonText;
			public Action[] Click;
		}
	
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] Transform controlRoot;
		[SerializeField] BuildingManageControlLeaf[] prefabs;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public event Action RefreshControls = ActionExtensions.Empty;
		
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

				for (var i = 0; i < control.LabelText.Length; i++)
				{
					instance.Labels[i].text = control.LabelText[i];
				}
				
				for (var i = 0; i < control.ButtonText.Length; i++)
				{
					instance.ButtonLabels[i].text = control.ButtonText[i];
				}
				
				for (var i = 0; i < control.Click.Length; i++)
				{
					var click = control.Click[i];
					if (click != null)
					{
						instance.Buttons[i].onClick.AddListener(() => click());
						instance.Buttons[i].onClick.AddListener(OnRefreshControls);
					}
				}
			}
		}
		#endregion

		public override void Cleanup()
		{
			base.Cleanup();

			RefreshControls = ActionExtensions.Empty;
			
			foreach (var prefab in prefabs) prefab.gameObject.SetActive(false);

			Controls();
		}

		#region Events
		void OnRefreshControls() => RefreshControls();
		#endregion
	}
 
}