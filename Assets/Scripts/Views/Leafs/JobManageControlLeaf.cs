using System;
using Lunra.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lunra.Hothouse.Views
{
	public class JobManageControlLeaf : MonoBehaviour
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] TextMeshProUGUI nameLabel;
		[SerializeField] TextMeshProUGUI countLabel;
		
		[SerializeField] GameObject[] controlOptionRoots;
		
		[SerializeField] Button increaseButton;
		[SerializeField] Button decreaseButton;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Bindings
		public string Name { set => nameLabel.text = value ?? String.Empty; }
		public int Count { set => countLabel.text = value.ToString(); }

		public bool ControlsEnabled
		{
			set
			{
				foreach (var control in controlOptionRoots) control.SetActive(value);
			}
		}
		public bool IncreaseEnabled { set => increaseButton.interactable = value; }
		public bool DecreaseEnabled { set => decreaseButton.interactable = value; }
		public Action IncreaseClick = ActionExtensions.Empty;
		public Action DecreaseClick = ActionExtensions.Empty;
		#endregion

		#region Events
		public void OnIncreaseClick() => IncreaseClick();
		public void OnDecreaseClick() => DecreaseClick();
		#endregion
	}
}