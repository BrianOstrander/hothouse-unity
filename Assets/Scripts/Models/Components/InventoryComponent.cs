using System.Linq;
using Lunra.Satchel;
using UnityEngine;

namespace Lunra.Hothouse.Models
{
	public interface IInventoryModel : IRoomTransformModel
	{
		InventoryComponent Inventory { get; }
	}
	
	public class InventoryComponent : ComponentModel<IInventoryModel>
	{
		#region Serialized
		public Inventory Container { get; private set; }
		#endregion
		
		#region Non Serialized
		#endregion

		public override void Bind()
		{
			if (Model is IHealthModel health) health.Health.Destroyed += OnHealthDestroyed;
		}
		
		public override void UnBind()
		{
			if (Model is IHealthModel health) health.Health.Destroyed -= OnHealthDestroyed;
			
			// I think this is the correct place to destroy any items, should ensure they are properly cleaned up...
			Container.Reset();
		}

		protected override void OnInitialize() => Container = Container?.Initialize(Game.Items) ?? Game.Items.Builder.Inventory();
		
		#region HealthComponent Events
		void OnHealthDestroyed(Damage.Result result)
		{
			var droppedItems = Container
				.All(i => i[Items.Keys.Shared.Type] == Items.Values.Shared.Types.Resource)
				.Select(e => e.Stack)
				.ToArray();

			if (droppedItems.Any())
			{
				Game.ItemDrops.Activate(
					Model,
					Quaternion.identity,
					Container.Withdrawal(droppedItems)
				);
			}
		}
		#endregion

		public void Reset(ItemStore itemStore)
		{
			ResetId();
		}

		public override string ToString()
		{
			var result = "Inventory Component [ " + ShortId + " ]:\n";
			result += Container.ToString(Inventory.Formats.IncludeItems | Inventory.Formats.IncludeItemProperties);
			return result;
		}
	}
}