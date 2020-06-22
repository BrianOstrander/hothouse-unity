using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.StyxMvp;
using UnityEngine;
using UnityEngine.UI;

namespace Lunra.Hothouse.Views
{
	public class ToolbarView : View
	{
		public struct Control
		{
			public string Label;
			public string Id;
		}
		
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] Color selectedColor;
		[SerializeField] Color notSelectedColor;

		[SerializeField] ToolbarControlLeaf controlPrefab;
		[SerializeField] GameObject controlsRoot;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		public event Action<string> Selection;
		
		public void InitializeControls(params Control[] controls)
		{
			entries.Clear();
			controlsRoot.transform.ClearChildren();

			foreach (var control in controls)
			{
				var instance = controlsRoot.InstantiateChild(
					controlPrefab,
					setActive: true
				);

				instance.Label = control.Label;
				instance.Click += () => OnSelectionClick(control.Id);
				
				entries.Add(control.Id, instance);
			}
		}

		public void SetSelection(string id = null)
		{
			foreach (var entry in entries)
			{
				entry.Value.SelectionColor = entry.Key == id ? selectedColor : notSelectedColor;
			}
		}
		#endregion

		Dictionary<string, ToolbarControlLeaf> entries = new Dictionary<string, ToolbarControlLeaf>();
		
		public override void Cleanup()
		{
			base.Cleanup();

			controlPrefab.gameObject.SetActive(false);
			Selection = ActionExtensions.GetEmpty<string>();
			
			InitializeControls();
		}

		#region Events
		void OnSelectionClick(string label) => Selection(label);
		#endregion
	}

}