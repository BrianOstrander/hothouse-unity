using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class ModifierDefinition
	{
		public static ModifierDefinition Stacking(string tag, float value) => new ModifierDefinition(tag, value, Rules.Stack);
		public static ModifierDefinition NoStacking(string tag, float value) => new ModifierDefinition(tag, value, Rules.NoStack);

		public enum Rules
		{
			Unknown = 0,
			Stack = 10,
			NoStack = 20
		}
			
		[JsonProperty] public string Tag { get; private set; }
		[JsonProperty] public float Value { get; private set; }
		[JsonProperty] public Rules Rule { get; private set; }
		
		[JsonConstructor]
		ModifierDefinition(
			string tag,
			float value,
			Rules rule
		)
		{
			Tag = tag;
			Value = value;
			Rule = rule;
		}
	}
}