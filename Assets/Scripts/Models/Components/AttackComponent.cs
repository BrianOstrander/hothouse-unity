using Newtonsoft.Json;
using System.Linq;
using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public interface IAttackModel : IAgentInventoryModel, IHealthModel, ITagModel
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
		#endregion

		public override void Bind()
		{
			Game.SimulationUpdate += OnGameSimulationUpdate;
			Model.Inventory.All.Changed += OnParentInventory;
			Model.Tags.All.Changed += OnParentTags;
			
		}

		public override void UnBind()
		{
			Game.SimulationUpdate -= OnGameSimulationUpdate;
			Model.Inventory.All.Changed -= OnParentInventory;
			Model.Tags.All.Changed -= OnParentTags;
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
		void OnParentInventory(Inventory inventory)
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
				result += "\n - " + attack;
			}
			
			return result;
		}
	}
}