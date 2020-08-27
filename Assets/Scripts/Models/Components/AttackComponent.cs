using Newtonsoft.Json;
using System.Linq;
using Lunra.Core;
using Lunra.Satchel;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IAttackModel : IInventoryModel, IHealthModel, ITagModel
	{
		AttackComponent Attacks { get; }
	}

	public class AttackComponent : ComponentModel<IAttackModel>
	{
		#region Serialized
		[JsonProperty] public Attack[] All { get; private set; } = new Attack[0];
		[JsonProperty] bool anyCooldownRequired;
		[JsonProperty] bool anyParentInventoryRequired;
		[JsonProperty] bool anyTagsRequired;
		[JsonProperty] DayTime? nextCooldownExpires;
		#endregion
		
		#region Non Serialized
		[JsonIgnore] public string LastAttackId { get; set; }
		#endregion

		public override void Bind()
		{
			Game.SimulationUpdate += OnGameSimulationUpdate;
			// Model.Inventory.All.Changed += OnParentInventory;
			Debug.LogError("TODO: Bind inventory");
			Model.Tags.All.Changed += OnParentTags;
			
		}

		public override void UnBind()
		{
			Game.SimulationUpdate -= OnGameSimulationUpdate;
			// Model.Inventory.All.Changed -= OnParentInventory;
			Debug.LogError("TODO: UnBind inventory");
			Model.Tags.All.Changed -= OnParentTags;
		}

		public bool TryGetMostEffective(
			IHealthModel target,
			out Attack attack,
			FloatRange? distance = null,
			DayTime? simulatedTime = null
		)
		{
			var mostEffectiveValue = float.MinValue;
			attack = null;

			var attackDistance = distance ?? FloatRange.Constant(Model.DistanceTo(target));
			var attackSimulatedTime = simulatedTime ?? Game.SimulationTime.Value;
			
			foreach (var currentAttack in All)
			{
				var anyEffectiveness = currentAttack.TryGetEffectiveness(
					Model,
					target,
					attackDistance,
					attackSimulatedTime,
					out var effectiveness
				);

				if (anyEffectiveness && mostEffectiveValue < effectiveness)
				{
					mostEffectiveValue = effectiveness;
					attack = currentAttack;
				}
			}

			return 0f < mostEffectiveValue && attack != null;
		}

		#region GameModel Events
		void OnGameSimulationUpdate()
		{
			if (!anyCooldownRequired) return;
			if (Game.SimulationTime.Value < nextCooldownExpires) return;

			foreach (var attack in All) attack.Update(Game, Model);
			
			CalculateNextCooldown();
		}
		#endregion
		
		#region Parent Events
		void OnParentInventory(Inventory.Event delta)
		{
			if (!anyParentInventoryRequired) return;

			foreach (var attack in All.Where(a => a.IsParentInventoryRequired)) attack.Update(Game, Model);
		}

		void OnParentTags(TagComponent.Entry[] tags)
		{
			if (!anyTagsRequired) return;

			foreach (var attack in All.Where(a => a.IsTagsRequired)) attack.Update(Game, Model);
		}
		#endregion

		void CalculateNextCooldown() => nextCooldownExpires = All.Max(a => a.CooldownExpired);

		public void Reset(
			params Attack[] all
		)
		{
			All = all;

			anyCooldownRequired = all.Any(a => a.IsCooldownRequired);
			anyParentInventoryRequired = all.Any(a => a.IsParentInventoryRequired);
			anyTagsRequired = all.Any(a => a.IsTagsRequired);
			
			nextCooldownExpires = DayTime.Zero;
		}

		public override string ToString()
		{
			var result = "Attacks: ";

			if (All.None()) return result + "None";

			foreach (var attack in All)
			{
				result += $"\n {((attack.State.HasFlag(Attack.States.WaitingForCooldown) && attack.Id == LastAttackId) ? " > " : " - ")} {attack}";
			}
			
			return result;
		}
	}
}