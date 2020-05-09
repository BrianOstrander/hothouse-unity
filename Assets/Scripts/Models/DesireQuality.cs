using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lunra.Hothouse.Models
{
	public struct DesireQuality
	{
		public static DesireQuality New(
			Desires desire,
			float quality
		)
		{
			return new DesireQuality(
				desire,
				Inventory.Empty,
				quality,
				States.Available
			);
		}
		
		public enum States
		{
			Unknown = 0,
			Available = 10,
			NotAvailable = 20
		}
		
		public readonly Desires Desire;
		public readonly Inventory Cost;
		public readonly float Quality;
		public readonly States State;

		public DesireQuality(
			Desires desire,
			Inventory cost,
			float quality,
			States state = States.Unknown
		)
		{
			Desire = desire;
			Cost = cost;
			Quality = quality;
			State = state;
		}

		public DesireQuality CalculateState(Inventory availableItems)
		{
			return new DesireQuality(
				Desire,
				Cost,
				Quality,
				availableItems.Contains(Cost) ? States.Available : States.NotAvailable
			);
		}
	}

	public static class DesireQualityLinqExtensions
	{
		public static float FirstAvailableQualityOrDefault(this IEnumerable<DesireQuality> entries, Desires desire)
		{
			return entries.FirstOrDefault(e => e.Desire == desire && e.State == DesireQuality.States.Available).Quality;
		}
	}
}