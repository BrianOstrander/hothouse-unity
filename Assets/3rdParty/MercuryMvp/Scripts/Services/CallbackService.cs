using System;

using UnityEngine.SceneManagement;

namespace LunraGames.SubLight
{
	public class CallbackService
	{
		#region Events
		/// <summary>
		/// When the escape key is released.
		/// </summary>
		public Action Escape = ActionExtensions.Empty;
		/// <summary>
		/// The state changed.
		/// </summary>
		public Action<StateChange> StateChange = ActionExtensions.GetEmpty<StateChange>();
		/// <summary>
		/// Called when any interactable elemnts are added to the world, and 
		/// false when all are removed.
		/// </summary>
		public Action<bool> Interaction = ActionExtensions.GetEmpty<bool>();
		#endregion

	}
}