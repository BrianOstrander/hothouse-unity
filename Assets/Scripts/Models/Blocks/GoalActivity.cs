using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Lunra.Satchel;

namespace Lunra.Hothouse.Models
{
	public struct GoalActivity
	{
		[JsonProperty] public string Id { get; private set; }
		[JsonProperty] public string Type { get; private set; }
		[JsonProperty] public (Motives Motive, float InsistenceModifier)[] Modifiers { get; private set; }
		[JsonProperty] public DayTime Duration { get; private set; }
		[JsonProperty] public Stack[] Input { get; private set; }
		[JsonProperty] public Stack[] Output { get; private set; }
		[JsonProperty] public bool RequiresOwnership { get; private set; }

		public GoalActivity(
			string type,
			(Motives Motive, float InsistenceModifier)[] modifiers,
			DayTime? duration = null,
			Stack[] input = null,
			Stack[] output = null,
			bool requiresOwnership = false
		)
		{
			Id = Guid.NewGuid().ToString();
			Type = type;
			Modifiers = modifiers;
			Duration = duration ?? DayTime.FromHours(1f);
			Input = input;
			Output = output;
			RequiresOwnership = requiresOwnership;
		}
	}
}