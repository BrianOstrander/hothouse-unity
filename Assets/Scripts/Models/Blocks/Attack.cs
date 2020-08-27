using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Lunra.Core;
using Lunra.Satchel;
using Lunra.StyxMvp.Models;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public class Attack
	{
		[Flags]
		public enum States
		{
			Available = 0,
			WaitingForInitialize = 1 << 0,
			WaitingForInput = 1 << 1,
			WaitingForOutput = 1 << 2,
			WaitingForCooldown = 1 << 3,
			WaitingForTags = 1 << 4
		}

		public enum OutputLocations
		{
			Unknown = 0,
			ParentInventory = 10,
			ParentDrop = 20,
			TargetDrop = 30
		}
		
		[JsonProperty] public string Id { get; private set; }
		[JsonProperty] public string Name { get; private set; }
		[JsonProperty] public FloatRange Range { get; private set; }
		[JsonProperty] public float Damage { get; private set; }
		[JsonProperty] public DayTime Duration { get; private set; }
		[JsonProperty] public DayTime Cooldown { get; private set; }
		[JsonProperty] public Damage.Types DamageType { get; private set; }
		[JsonProperty] public Stack[] InputItems { get; private set; }
		[JsonProperty] public ReadOnlyDictionary<OutputLocations, Stack[]> OutputItems { get; private set; }
		[JsonProperty] public ReadOnlyDictionary<string, bool> RequiredTags { get; private set; }
		
		[JsonProperty] public States State { get; private set; }
		[JsonProperty] public bool IsCooldownRequired { get; private set; }
		[JsonProperty] public bool IsParentInventoryRequired { get; private set; }
		[JsonProperty] public bool IsTagsRequired { get; private set; }
		[JsonProperty] public DayTime CooldownExpired { get; private set; }

		public Attack(
			string id,
			string name,
			FloatRange range,
			float damage,
			DayTime duration,
			DayTime? cooldown = null,
			Damage.Types damageType = Models.Damage.Types.Generic,
			Stack[] inputItems = null,
			ReadOnlyDictionary<OutputLocations, Stack[]> outputItems = null,
			ReadOnlyDictionary<string, bool> requiredTags = null
		)
		{
			Id = id;
			Name = name;
			Range = range;
			Damage = damage;
			Duration = duration;
			Cooldown = cooldown ?? DayTime.Zero;
			DamageType = damageType;
			InputItems = inputItems ?? new Stack[0];
			OutputItems = outputItems ?? new ReadOnlyDictionary<OutputLocations, Stack[]>(new Dictionary<OutputLocations, Stack[]>());
			RequiredTags = requiredTags ?? new ReadOnlyDictionary<string, bool>(new Dictionary<string, bool>());
			
			State = States.WaitingForInitialize;
			IsCooldownRequired = DayTime.Zero < Duration || DayTime.Zero < Cooldown;
			IsParentInventoryRequired = InputItems.Any() || OutputItems.Any(kv => kv.Key == OutputLocations.ParentInventory);
			IsTagsRequired = RequiredTags.Any();
		}

		public void Update(
			GameModel game,
			IAttackModel model
		)
		{
			State = States.Available;

			if (IsParentInventoryRequired)
			{
				Debug.LogError("TODO: Check if inventory has input required");
				// if (InputItems.Any() && !model.Inventory.All.Value.Contains(InputItems)) State |= States.WaitingForInput;

				if (OutputItems.TryGetValue(OutputLocations.ParentInventory, out var output))
				{
					Debug.LogError("TODO: Output capacity testing");
					// if (!model.Inventory.AllCapacity.Value.GetCapacityFor(model.Inventory.All.Value).Contains(output)) State |= States.WaitingForOutput;
				}
			}

			if (game.SimulationTime.Value < CooldownExpired) State |= States.WaitingForCooldown;

			if (IsTagsRequired)
			{
				foreach (var tag in RequiredTags)
				{
					if (model.Tags.Contains(tag.Key) != tag.Value)
					{
						State |= States.WaitingForTags;
						break;
					}
				}
			}
		}

		public bool TryGetEffectiveness(
			IAttackModel source,
			IHealthModel target,
			FloatRange distance,
			DayTime simulatedTime,
			out float effectiveness
		)
		{
			effectiveness = float.MinValue;

			if (State != States.Available)
			{
				if (State != States.WaitingForCooldown || simulatedTime < CooldownExpired) return false;
			}
			
			if (!Range.Intersects(distance)) return false;

			var simulatedDamage = Models.Damage.Simulate(
				DamageType,
				Damage,
				source,
				target
			);

			effectiveness = simulatedDamage.AmountAbsorbed;

			// TODO: Take into account travel time...
			
			if (0f < Duration.TotalTime) effectiveness /= Duration.TotalTime;
			
			return 0f < effectiveness;
		}

		public Damage.Result Trigger(
			GameModel game,
			IAttackModel source,
			IHealthModel target
		)
		{
			CooldownExpired = Duration + Cooldown + game.SimulationTime.Value;

			// if (InputItems.Any()) source.Inventory.Remove(InputItems);
			Debug.LogError("TODO: remove input items");

			foreach (var output in OutputItems)
			{
				switch (output.Key)
				{
					case OutputLocations.ParentInventory:
						// source.Inventory.Add(output.Value);
						Debug.LogError("TODO: add output items");
						break;
					case OutputLocations.ParentDrop:
						game.ItemDrops.Activate(
							source,
							Quaternion.identity,
							output.Value
						);
						break;
					case OutputLocations.TargetDrop:
						game.ItemDrops.Activate(
							target,
							Quaternion.identity,
							output.Value
						);
						break;
					default:
						Debug.LogError("Unrecognized OutputLocation: "+output.Key);
						break;
				}
			}

			var result = Models.Damage.Apply(
				DamageType,
				Damage,
				source,
				target
			);

			source.Attacks.LastAttackId = Id;
			
			Update(game, source);
			
			return result;
		}

		public override string ToString()
		{
			var result = $"[ {Model.ShortenId(Id)} ] ";
			result += $"{StringExtensions.GetNonNullOrEmpty(Name, "< null or empty name >")} ";

			var waitingResult = string.Empty;

			foreach (var state in EnumExtensions.GetValues(States.Available))
			{
				if (State.HasFlag(state)) waitingResult += $" {state.ToString().Replace("WaitingFor", "")}";
			}

			if (!string.IsNullOrEmpty(waitingResult)) result += $"| Waiting For ({waitingResult} )";
			
			return result;
		}
	}
}