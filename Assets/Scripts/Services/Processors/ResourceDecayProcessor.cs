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
			.RequireAll(PropertyValidations.Bool.EqualTo(Items.Keys.Resource.Decay.IsEnabled, true));

		public override void Process(Item item, float deltaTime)
		{
			var current = item[Items.Keys.Resource.Decay.Current];

			if (CheckForDestruction(item, current)) return;

			// The following looks a bit weird, but stops us from doing calculations we don't need when decay is at
			// zero but we can't destroy this item -- for example, when it is in transit or in use by logistics.
			
			var rateSinceLastTime = 0f;

			var isAlreadyAtZero = Mathf.Approximately(0f, current) && Mathf.Approximately(0f, item[Items.Keys.Resource.Decay.Previous]);
			
			if (!isAlreadyAtZero)
			{
				var rate = item[Items.Keys.Resource.Decay.Rate];
			
				// TODO: Additional rate modifiers calculated here, probably obtained from inventory...

				rate *= deltaTime;

				var previous = current;
				current = Mathf.Max(0f, current - rate);

				rateSinceLastTime = Mathf.Abs(current - previous) / deltaTime;

				item[Items.Keys.Resource.Decay.Previous] = previous;
				item[Items.Keys.Resource.Decay.Current] = current;

				// We can skip the final rate prediction set if we get destroyed...
				if (CheckForDestruction(item, current)) return;
			}

			item[Items.Keys.Resource.Decay.RatePredicted] = (PredictedRatePastWeight * item[Items.Keys.Resource.Decay.RatePredicted])
			                                                + (PredictedRateRecentWeight * rateSinceLastTime);
		}

		bool CheckForDestruction(
			Item item,
			float current
		)
		{ 
			if (Mathf.Approximately(0f, current))
			{
				if (item[Items.Keys.Resource.IsPromised]) return false;
				if (item[Items.Keys.Resource.LogisticState] != Items.Values.Resource.LogisticStates.None) return false;
				
				OnDestruction(item);
				return true;
			}

			return false;
		}

		void OnDestruction(Item item)
		{
			if (!ItemStore.Containers.TryGetValue(item.ContainerId, out var inventory))
			{
				Debug.LogError($"Unable to get find an inventory with an Id of {item.ContainerId}");
				return;
			}
			
			inventory.Destroy(item);
		}

		public override bool BreakProcessing(Item item) => item.NoInstances;
	}
}