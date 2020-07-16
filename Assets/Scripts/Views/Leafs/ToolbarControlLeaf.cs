using System;
using Lunra.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lunra.Hothouse.Views
{
	public class ToolbarControlLeaf : MonoBehaviour
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] TextMeshProUGUI label;
		[SerializeField] Graphic selectedGraphic;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Bindings
		public string Label { set => label.text = value ?? String.Empty; }
		public Color SelectionColor { set => selectedGraphic.color = value; }

		public Action Click;
		#endregion

		#region Events
		public void OnClick() => Click();
		#endregion
	}
}