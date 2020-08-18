using System;
using System.Collections.Generic;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

namespace Lunra.Hothouse.Models
{
	public class BubblerModel : AgentModel, IClearableModel
	{
		#region Serialized
		[JsonProperty] public LightSensitiveComponent LightSensitive { get; private set; } = new LightSensitiveComponent();
		[JsonProperty] public ClearableComponent Clearable { get; private set; } = new ClearableComponent();
		[JsonProperty] public ObligationComponent Obligations { get; private set; } = new ObligationComponent();
		[JsonProperty] public EnterableComponent Enterable { get; private set; } = new EnterableComponent();
		#endregion
		
		#region Non Serialized
		#endregion

		public BubblerModel()
		{
			AppendComponents(
				LightSensitive,
				Clearable,
				Obligations,
				Enterable
			);
		}
	}
}