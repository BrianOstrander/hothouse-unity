using System;
using Lunra.Core;
using Lunra.StyxMvp;
using UnityEngine;

namespace Lunra.Hothouse.Views
{
	public class AgentView : PrefabView
	{
		#region Serialized
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value null
		[SerializeField] string deathEffectId;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value null
		#endregion

		#region Bindings
		#endregion
		
		#region Reverse Bindings
		public string DeathEffectId => deathEffectId;
		#endregion
	}

}