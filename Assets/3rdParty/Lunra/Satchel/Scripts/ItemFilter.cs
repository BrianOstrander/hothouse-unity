using System;
using Newtonsoft.Json;

namespace Lunra.Satchel
{
	public class ItemFilter
	{
		[JsonProperty] public PropertyValidation[] All { get; private set; }
		[JsonProperty] public PropertyValidation[] None { get; private set; }
		[JsonProperty] public PropertyValidation[] Any { get; private set; }

		ItemStore itemStore;
		
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
			this.itemStore = itemStore ?? throw new ArgumentNullException(nameof(itemStore));

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
	}
}