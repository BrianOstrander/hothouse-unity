using Newtonsoft.Json;
using System.Linq;
using Lunra.Satchel;

namespace Lunra.Hothouse.Models
{
	public class Recipe
	{
		public string Id => Name;
		[JsonProperty] public string Name { get; private set; }
		[JsonProperty] public Stack[] InputItems { get; private set; }
		[JsonProperty] public Stack[] OutputItems { get; private set; }
		[JsonProperty] public DayTime Duration { get; private set; }

		public Recipe(
			string name,
			Stack[] inputItems,
			Stack[] outputItems,
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

			result += "TODO";

			result += "\n\tOutput:";

			result += "TODO";

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