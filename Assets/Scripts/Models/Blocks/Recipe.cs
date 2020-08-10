using Newtonsoft.Json;
using System.Linq;

namespace Lunra.Hothouse.Models
{
	public class Recipe
	{
		public string Id => Name;
		[JsonProperty] public string Name { get; private set; }
		[JsonProperty] public Inventory InputItems { get; private set; }
		[JsonProperty] public Inventory OutputItems { get; private set; }
		[JsonProperty] public DayTime Duration { get; private set; }

		public Recipe(
			string name,
			Inventory inputItems,
			Inventory outputItems,
			DayTime duration
		)
		{
			Name = name;
			InputItems = inputItems;
			OutputItems = outputItems;
			Duration = duration;
		}

		public override string ToString()
		{
			var result = string.IsNullOrEmpty(Name) ? "< null or empty name >" : Name;

			result += "\n\tInput:";

			foreach (var entry in InputItems.Entries.Where(e => 0 < e.Weight))
			{
				result += "\n\t\t" + entry.Type + "\t" + entry.Weight;
			}

			result += "\n\tOutput:";
			
			foreach (var entry in OutputItems.Entries.Where(e => 0 < e.Weight))
			{
				result += "\n\t\t" + entry.Type + "\t" + entry.Weight;
			}

			result += "\n\tDuration: ";

			if (Duration.Day < 0)
			{
				Duration.GetYearDayHourMinutePadded(
					out _,
					out _,
					out _,
					out var hours,
					out var minutes
				);

				result += $"{hours} : {minutes}";
			}
			else result += Duration.ToString();
			
			return result;
		}
	}
}