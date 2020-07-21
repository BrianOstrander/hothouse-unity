using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lunra.Hothouse.Models
{
	public struct DesireQuality
	{
		public static DesireQuality New(
			Motives motive,
			float quality
		)
		{
			return new DesireQuality(
				motive,
				Inventory.Empty,
				quality,
				States.Available
			);
		}
		
		public static DesireQuality New(
			Motives motive,
			float quality,
			Inventory inventory
		)
		{
			return new DesireQuality(
				motive,
				inventory,
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
		
		public readonly Motives Motive;
		public readonly Inventory Cost;
		public readonly float Quality;
		public readonly States State;

		public DesireQuality(
			Motives motive,
			Inventory cost,
			float quality,
			States state = States.Unknown
		)
		{
			Motive = motive;
			Cost = cost;
			Quality = quality;
			State = state;
		}

		public DesireQuality CalculateState(Inventory availableItems)
		{
			return new DesireQuality(
				Motive,
				Cost,
				Quality,
				availableItems.Contains(Cost) ? States.Available : States.NotAvailable
			);
		}
	}

	public static class DesireQualityLinqExtensions
	{
		public static float FirstAvailableQualityOrDefault(this IEnumerable<DesireQuality> entries, Motives motive)
		{
			return entries.FirstOrDefault(e => e.Motive == motive && e.State == DesireQuality.States.Available).Quality;
		}
	}
}