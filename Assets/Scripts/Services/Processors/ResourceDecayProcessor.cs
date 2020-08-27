using Lunra.Hothouse.Models;
using Lunra.Satchel;
using UnityEngine;

namespace Lunra.Hothouse.Services
{
	public class ResourceDecayProcessor : Processor
	{
		const float PredictedRatePastWeight = 0.95f;
		const float PredictedRateRecentWeight = 1f - PredictedRatePastWeight;
		
		protected override PropertyFilter GetFilter() => ItemStore.Builder
			.BeginPropertyFilter()
			.RequireAll(PropertyValidation.Default.Bool.EqualTo(ItemKeys.Resource.Decay.Enabled.Key, true));

		public override void Process(Item item, float deltaTime)
		{
			var current = item.Get(ItemKeys.Resource.Decay.Current);
			if (Mathf.Approximately(0f, current)) return;

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
		}
	}
}