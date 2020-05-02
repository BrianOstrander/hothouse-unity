using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using UnityEngine;

namespace Lunra.WildVacuum.Models
{
	public struct Inventory
	{
		public static Inventory Default => new Inventory(new Capacity[0], Resources.Unknown, 0);
		
		public struct Capacity
		{
			public readonly Resources Resource;
			public readonly int Count;

			public Capacity(
				Resources resource,
				int count
			)
			{
				Resource = resource;
				Count = count;
			}

			public override string ToString() => Resource + " : " + Count;
		}

		public readonly Capacity[] Storage;
		public readonly Resources Resource;
		public readonly int Count;
		public readonly int Maximum;
		public readonly bool IsFull;

		public Inventory(
			Capacity[] storage,
			Resources resource,
			int count
		)
		{
			Storage = storage;
			Resource = resource;
			Count = count;
			Maximum = storage.FirstOrDefault(s => s.Resource == resource).Count;

			IsFull = resource != Resources.Unknown && storage.FirstOrDefault(s => s.Resource == resource).Count <= count;
		}

		public int ModifyStorage(
			int count,
			Resources resource,
			out Inventory inventory
		)
		{
			if (resource == Resources.Unknown)
			{
				Debug.LogError("Cannot modify storage of " + resource);
				inventory = this;
				return 0;
			}
			
			var newStorage = Storage.Where(s => s.Resource != resource).Append(new Capacity(resource, count));

			var remaining = 0;
			var newCount = Count;
			var newResource = Resource;
			
			if (resource == Resource && count < Count)
			{
				remaining = Count - count;
				newCount = count;
			}
			
			inventory = new Inventory(
				newStorage.ToArray(),
				newCount == 0 ? Resources.Unknown : newResource,
				newCount
			);

			return remaining;
		}
		
		public int ModifyCount(
			int count,
			out Inventory inventory
		)
		{
			var remaining = 0;
			var newCount = count;

			if (Maximum < count)
			{
				remaining = count - Maximum;
				newCount = Maximum;
			}
			
			inventory = new Inventory(
				Storage,
				newCount == 0 ? Resources.Unknown : Resource,
				newCount
			);

			return remaining;
		}
		
		public Capacity ModifyResourceCount(
			int count, 
			Resources resource,
			out Inventory inventory
		)
		{
			if (resource == Resources.Unknown)
			{
				if (count != 0)
				{
					Debug.LogError("Unable to modify resource count for "+resource);
					inventory = this;
					return default;
				}
				
				inventory = new Inventory(
					Storage,
					resource,
					count
				);
				
				return new Capacity(Resource, Count);
			}

			if (resource == Resource)
			{
				return new Capacity(resource, ModifyCount(count, out inventory));
			}

			var maximum = Storage.FirstOrDefault(s => s.Resource == resource).Count;

			var remaining = 0;
			var newConut = count;
			
			if (maximum < count)
			{
				remaining = count - maximum;
				newConut = maximum;
			}
			
			inventory = new Inventory(
				Storage,
				resource,
				newConut
			);

			return new Capacity(Resource, remaining);
		}

		public Inventory Empty()
		{
			return new Inventory(
				Storage,
				Resources.Unknown,
				0
			);
		}

		public int Fill(
			int count,
			Resources resource,
			out Inventory inventory
		)
		{
			if (resource == Resources.Unknown)
			{
				inventory = this;
				Debug.LogError("Cannot fill with "+resource);
				return count;
			}

			if (Resource != Resources.Unknown && resource != Resource)
			{
				inventory = this;
				return count;
			}

			var capacity = Storage.FirstOrDefault(s => s.Resource == resource);

			var remaining = count - Mathf.Min(capacity.Count - Count, count);
			
			inventory = new Inventory(
				Storage,
				resource,
				Count + (count - remaining)
			);

			return remaining;
		}

		public override string ToString()
		{
			var result = Resource + " : " + Count + "\n______";
			foreach (var capacity in (Storage ?? new Capacity[0])) result += "\n" + capacity.Resource + " : " + capacity.Count;
			return result + "\n______";
		}
	}
}