using Lunra.Core;
using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
	public class Obligation
	{
		public static Obligation New(
			string type
		)
		{
			return new Obligation(
				type
			);
		}
		
		[JsonProperty] public string Type { get; private set; }

		[JsonConstructor]
		Obligation(
			string type
		)
		{
			Type = type;
		}

		public bool Is(Obligation obligation) => obligation.Type == Type; 

		public override string ToString()
		{
			return StringExtensions.GetNonNullOrEmpty(Type, "< null or empty type >");
		}
	}
}