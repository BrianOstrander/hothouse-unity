using System;
using Lunra.Hothouse.Models;
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

		JobManageView.ControlEntry control;

		public JobManageView.ControlEntry Control
		{
			set
			{
				control = value;
				nameLabel.text = control.Name ?? String.Empty;
				UpdateCount(control.GetCount?.Invoke());
			}
		}

		void UpdateCount(JobManageView.ControlClickResult? result)
		{
			var anyControlOptionEnabled = false;
			
			if (result.HasValue)
			{
				countLabel.text = result.Value.Count.ToString();
				increaseButton.interactable = result.Value.IsIncreaseEnabled;
				decreaseButton.interactable = result.Value.IsDecreaseEnabled;
				anyControlOptionEnabled = result.Value.IsIncreaseEnabled || result.Value.IsDecreaseEnabled;
			}
			else
			{
				countLabel.text = "0";
			}

			foreach (var controlOption in controlOptionRoots) controlOption.SetActive(anyControlOptionEnabled);
		}

		void ModifyCount(int amount) => UpdateCount(control.ModifyCount?.Invoke(amount));

		#region Events
		public void OnIncreaseClick() => ModifyCount(1);
		public void OnDecreaseClick() => ModifyCount(-1);
		#endregion
	}
}