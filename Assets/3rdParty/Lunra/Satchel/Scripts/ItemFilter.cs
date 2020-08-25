using System;
using Lunra.Core;
using Newtonsoft.Json;

namespace Lunra.Satchel
{
	public class ItemFilter
	{
		[JsonProperty] public PropertyValidation[] All { get; private set; }
		[JsonProperty] public PropertyValidation[] None { get; private set; }
		[JsonProperty] public PropertyValidation[] Any { get; private set; }

		public ItemFilter(
			PropertyValidation[] all,
			PropertyValidation[] none,
			PropertyValidation[] any
		)
		{
			All = all ?? throw new ArgumentNullException(nameof(all));
			None = none ?? throw new ArgumentNullException(nameof(none));
			Any = any ?? throw new ArgumentNullException(nameof(any));
		}

		public ItemFilter Initialize(ItemStore itemStore)
		{
			if (itemStore == null) throw new ArgumentNullException(nameof(itemStore));

			foreach (var validation in All) validation.Initialize(itemStore);
			foreach (var validation in None) validation.Initialize(itemStore);
			foreach (var validation in Any) validation.Initialize(itemStore);

			return this;
		}

		public bool Validate(Item item)
		{
			foreach (var validation in All)
			{
				if (validation.Validate(item) == PropertyValidation.Results.InValid) return false;
			}

			foreach (var validation in None)
			{
				if (validation.Validate(item) == PropertyValidation.Results.Valid) return false;
			}

			var noAnyValidations = true;
			
			foreach (var validation in Any)
			{
				noAnyValidations = false;
				if (validation.Validate(item) == PropertyValidation.Results.Valid) return true;
			}

			return noAnyValidations;
		}

		public override string ToString() => $"Item Filter: All [ {All.Length} ] | None [ {None.Length} ] | Any [ {Any.Length} ]";
		
		public string ToStringVerbose()
		{
			var result = ToString();
			
			result += "\n\tAll : ";

			if (All.None()) result += "None";
			else
			{
				foreach (var validation in All) result += $"\n\t - {validation}";
			}
			
			result += "\n\tNone : ";
			
			if (None.None()) result += "None";
			else
			{
				foreach (var validation in None) result += $"\n\t - {validation}";
			}
			
			result += "\n\tAny : ";
			
			if (Any.None()) result += "None";
			else
			{
				foreach (var validation in Any) result += $"\n\t - {validation}";
			}

			return result;
		}
	}
}