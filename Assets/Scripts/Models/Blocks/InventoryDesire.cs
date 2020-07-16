using Newtonsoft.Json;

namespace Lunra.Hothouse.Models
{
#pragma warning disable CS0661 // Defines == or != operator but does not override Object.GetHashCode()
#pragma warning disable CS0659 // Overrides Object.Equals(object) but does not override Object.GetHashCode()
	public struct InventoryDesire
#pragma warning restore CS0659 // Overrides Object.Equals(object) but does not override Object.GetHashCode()
#pragma warning restore CS0661 // Defines == or != operator but does not override Object.GetHashCode()
	{
		public static InventoryDesire UnCalculated(
			Inventory storage
		)
		{
			return new InventoryDesire(
				true,
				storage,
				Inventory.Empty, 
				Inventory.Empty
			);
		}
	
		public static InventoryDesire New(
			Inventory storage,
			Inventory delivery,
			Inventory distribution
		)
		{
			return new InventoryDesire(
				true,
				storage,
				delivery,
				distribution
			);
		}
	
		public static InventoryDesire Ignored()
		{
			return new InventoryDesire(
				false,
				Inventory.Empty,
				Inventory.Empty,
				Inventory.Empty
			);
		}
		
		public readonly bool IsActive;
		public readonly Inventory Storage;
		public readonly Inventory Delivery;
		public readonly Inventory Distribution;

		[JsonIgnore]
		public bool AnyInventoriesNotEmpty
		{
			get
			{
				if (!Storage.IsEmpty) return true;
				if (!Delivery.IsEmpty) return true;
				if (!Distribution.IsEmpty) return true;

				return false;
			}
		}
		
		InventoryDesire(
			bool isActive, 
			Inventory storage,
			Inventory delivery,
			Inventory distribution
		)
		{
			IsActive = isActive;
			Storage = storage;
			Delivery = delivery;
			Distribution = distribution;
		}
		
		public bool Equals(InventoryDesire desire)
		{
			if (IsActive != desire.IsActive) return false;
			if (Storage != desire.Storage) return false;
			if (Delivery != desire.Delivery) return false;
			if (Distribution != desire.Distribution) return false;

			return true;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;

			return obj.GetType() == GetType() && Equals((InventoryDesire)obj);
		}

		public static bool operator ==(InventoryDesire desire0, InventoryDesire desire1)
		{
			if (Equals(desire0, desire1)) return true;
			if (Equals(desire0, null)) return false;
			if (Equals(desire1, null)) return false;
			return desire0.Equals(desire1);
		}

		public static bool operator !=(InventoryDesire desire0, InventoryDesire desire1) { return !(desire0 == desire1); }
	}
}