using Lunra.Hothouse.Models;
using Lunra.Satchel;
using UnityEngine;

namespace Lunra.Hothouse.Services
{
	public class ResourceDecayProcessor : Processor
	{
		const float PredictedRatePastWeight = 0.95f;
		const float PredictedRateRecentWeight = 1f - PredictedRatePastWeight;

		public override int Priority => ProcessorPriorities.ResourceDecay;

		protected override PropertyFilter GetFilter() => ItemStore.Builder
			.BeginPropertyFilter()
			.RequireAll(PropertyValidation.Default.Bool.EqualTo(ItemKeys.Resource.Decay.Enabled.Key, true));

		public override void Process(Item item, float deltaTime)
		{
			var current = item.Get(ItemKeys.Resource.Decay.Current);

			if (CheckForDestruction(item, current)) return;

			var rate = item.Get(ItemKeys.Resource.Decay.Rate);
			
			// TODO: Additional rate modifiers calculated here, probably obtained from inventory...

			rate *= deltaTime;

			var previous = current;
			current = Mathf.Max(0f, current - rate);

			var rateSinceLastTime = Mathf.Abs(current - previous) / deltaTime;

			item.Set(
				ItemKeys.Resource.Decay.RatePredicted,
				(PredictedRatePastWeight * item.Get(ItemKeys.Resource.Decay.RatePredicted))
				+ (PredictedRateRecentWeight * rateSinceLastTime)
			);

			item.Set(ItemKeys.Resource.Decay.Previous, previous);
			item.Set(ItemKeys.Resource.Decay.Current, current);

			CheckForDestruction(item, current);
		}

		bool CheckForDestruction(
			Item item,
			float current
		)
		{ 
			if (Mathf.Approximately(0f, current))
			{
				OnDestruction(item);
				return true;
			}

			return false;
		}

		void OnDestruction(Item item)
		{
			if (!ItemStore.Inventories.TryGetValue(item.InventoryId, out var inventory))
			{
				Debug.LogError($"Unable to get find an inventory with an Id of {item.InventoryId}");
				return;
			}
			
			inventory.Destroy(item);
		}

		public override bool BreakProcessing(Item item) => item.Get(Constants.Destroyed);
	}
}