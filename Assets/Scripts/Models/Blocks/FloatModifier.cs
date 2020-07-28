using System.Linq;
using Lunra.Core;

namespace Lunra.Hothouse.Models
{
	public struct FloatModifier
	{
		public struct KeyDefinition
		{
			public string Category { get; }
			public string Type { get; }

			public KeyDefinition
			(
				string category,
				string type
			)
			{
				Category = category;
				Type = type;
			}

			public override string ToString()
			{
				return StringExtensions.GetNonNullOrEmpty(Category, "< null or empty category >") + "." + StringExtensions.GetNonNullOrEmpty(Type, "< null or empty key >");
			}
		}
	
		public static FloatModifier Default() => New();
		public static FloatModifier New(params (KeyDefinition Key, float Value)[] values) => new FloatModifier(values);
		
		public (KeyDefinition Key, float Value)[] All { get; }
		public (string Category, float Value)[] Sums { get; }
		
		FloatModifier(
			(KeyDefinition Key, float Value)[] all
		)
		{
			All = all;

			Sums = all
				.Select(kv => kv.Key.Category).Distinct()
				.Select(c => (c, all.Sum(kv => kv.Key.Category == c ? kv.Value : 0f)))
				.ToArray();
		}

		public float GetSum(string category) => Sums.FirstOrDefault(s => s.Category == category).Value;

		public override string ToString()
		{
			if (All.None()) return "None";
			
			var result = $"Modifiers:\t{All.Length}";

			foreach (var sum in Sums)
			{
				result += $"\n\t{sum.Category}:\t{sum.Value}";

				foreach (var value in All.Where(v => v.Key.Category == sum.Category))
				{
					result += $"\n\t - {value.Key.Type}:\t{value.Value}";
				}
			}

			return result;
		}
	}
}