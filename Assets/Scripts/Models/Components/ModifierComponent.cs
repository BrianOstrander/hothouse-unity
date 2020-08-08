using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using Lunra.StyxMvp.Models;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class ModifierComponent : ComponentModel<ITagModel>
	{
		#region Serialized
		public ModifierDefinition[] Definitions { get; private set; } = new ModifierDefinition[0];
		public FloatRange Clamping { get; private set; }

		[JsonProperty] float sum;
        readonly ListenerProperty<float> sumListener;
        [JsonIgnore] public ReadonlyProperty<float> Sum { get; }
		#endregion
		
		#region NonSerialized
		#endregion
		
		public ModifierComponent()
		{
			Sum = new ReadonlyProperty<float>(
				value => sum = value,
				() => sum,
				out sumListener
			);
		}

		public void Reset(
			ModifierDefinition[] definitions,
			float minimum = float.MinValue,
			float maximum = float.MaxValue
		)
		{
			Definitions = definitions;
			Clamping = new FloatRange(minimum, maximum);
			sumListener.Value = 0f;
		}

		public void Bind() => Model.Tags.All.Changed += OnTagAll;
		
		public void UnBind() => Model.Tags.All.Changed -= OnTagAll;
		
		#region TagModel Events
		void OnTagAll(TagComponent.Entry[] all)
		{
			var newSum = 0f;

			foreach (var definition in Definitions)
			{
				var tags = all.Where(t => t.Tag == definition.Tag);
				
				if (tags.None()) continue;

				switch (definition.Rule)
				{
					case ModifierDefinition.Rules.Stack:
						newSum += tags.Count() * definition.Value;
						break;
					case ModifierDefinition.Rules.NoStack:
						newSum += definition.Value;
						break;
					default:
						Debug.LogError("Unrecognized Rule: "+definition.Rule);
						break;
				}
			}

			newSum = Mathf.Clamp(newSum, Clamping.Primary, Clamping.Secondary);

			if (!Mathf.Approximately(newSum, Sum.Value)) sumListener.Value = newSum;
		}
        #endregion

        public override string ToString()
        {
	        var result = "Modifier: " + Sum.Value.ToString("N2");

	        return result;
        }
	}
}