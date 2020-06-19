using System;
using System.Linq;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class ObligationIndicatorView : PrefabView
	{
		[Serializable]
		struct ActionEntry
		{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
			public string Action;
			public GameObject Root;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		}
		
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] ActionEntry[] actions = new ActionEntry[0];
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion
		
		#region Bindings
		public void SetAction(string action)
		{
			if (action == null) Debug.LogError("Trying to set a null action");
			else if (string.IsNullOrEmpty(action)) Debug.LogError("Trying to set an empty action");
			
			OnSetAction(action);
		}
		
		void OnSetAction(string action = null)
		{
			var found = false;
			foreach (var current in actions)
			{
				var currentIsActive = current.Action == action;
				current.Root.SetActive(currentIsActive);
				found |= currentIsActive;
			}
			
			if (!string.IsNullOrEmpty(action) && !found) Debug.LogError("Unable to find action matching: "+action);
		}
		
		#endregion

		public override void Cleanup()
		{
			base.Cleanup();
			
			OnSetAction();
		}
	}
}