using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lunra.Hothouse.Views
{
	public class BuildingManageControlLeaf : MonoBehaviour
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		public BuildingManageView.Control.Types Type;
		public TextMeshProUGUI[] Labels;
		public TextMeshProUGUI[] ButtonLabels;
		public Button[] Buttons;
		
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
	}
}