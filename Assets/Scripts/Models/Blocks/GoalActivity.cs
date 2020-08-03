using System;
using System.Collections.Generic;

namespace Lunra.Hothouse.Models
{
	public struct GoalActivity
	{
		public string Id { get; }
		public string Type { get; }
		public (Motives Motive, float InsistenceModifier)[] Modifiers { get; }
		public DayTime Duration { get; }
		public Inventory? Input { get; }
		public Inventory? Output { get; }
		public bool RequiresOwnership { get; }

		public GoalActivity(
			string type,
			(Motives Motive, float InsistenceModifier)[] modifiers,
			DayTime? duration = null,
			Inventory? input = null,
			Inventory? output = null,
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