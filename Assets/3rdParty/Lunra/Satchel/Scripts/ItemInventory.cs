using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Lunra.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Satchel
{
	public class ItemInventory
	{
		[Flags]
		public enum Formats
		{
			Default = 0,
			IncludeItems = 1 << 0,
			IncludeItemProperties = 1 << 1
		}
		
		public struct Event
		{
			[Flags]
			public enum Formats
			{
				Default = 0,
				IncludeStacks = 1 << 0
			}
			
			[Flags]
			public enum Types
			{
				None = 0,
				Addition = 1 << 0,
				Subtraction = 1 << 1
			}

			[JsonProperty] public DateTime UpdateTime { get; private set; }
			[JsonProperty] public Types Updates { get; private set; }
			[JsonProperty] public ReadOnlyDictionary<ulong, (int OldCount, int NewCount, int DeltaCount, Types Type)> StackEvents { get; private set; }
			
			public Event(
				DateTime updateTime,
				Types updates,
				ReadOnlyDictionary<ulong, (int OldCount, int NewCount, int DeltaCount, Types Type)> stackEvents
			)
			{
				UpdateTime = updateTime;
				Updates = updates;
				StackEvents = stackEvents;
			}

			public override string ToString() => ToString(Formats.Default);

			public string ToString(Formats format)
			{
				var result = $"Updated {StackEvents.Count} Inventory Item(s) |";

				foreach (var referenceType in EnumExtensions.GetValues(Types.None))
				{
					if (Updates.HasFlag(referenceType)) result += " " + referenceType;
				}

				result += $" | {UpdateTime}";
				
				if (format == Formats.Default) return result;

				foreach (var stackEvent in StackEvents)
				{
					result += $"\n\t - [ {stackEvent.Key} ]\t {stackEvent.Value.OldCount} -> {stackEvent.Value.NewCount} ( Delta : {stackEvent.Value.DeltaCount} | {stackEvent.Value.Type})";
				}

				return result;
			}
		}
		
		public struct OperationRequest<T>
		{
			[JsonProperty] public T Result { get; private set; }
			[JsonProperty] public T Value { get; private set; }

			public OperationRequest(
				T result,
				T value
			)
			{
				Result = result;
				Value = value;
			}
			
			public OperationResult<T> Continues(T result) => new OperationResult<T>(result, false);
			public OperationResult<T> Skips(T result) => new OperationResult<T>(result, true);
		}
		
		public struct OperationResult<T>
		{
			[JsonProperty] public T Result { get; private set; }
			[JsonProperty] public bool SkipRemaining { get; private set; }

			public OperationResult(
				T result,
				bool skipRemaining
			)
			{
				Result = result;
				SkipRemaining = skipRemaining;
			}
		}
		
		#region Serialized
		[JsonProperty] List<ItemStack> stacks = new List<ItemStack>();
		[JsonProperty] DateTime lastUpdated;
		#endregion

		#region Non Serialized
		bool isInitialized;
		ItemStore itemStore;
		[JsonIgnore] public ReadOnlyCollection<ItemStack> Stacks { get; private set; }
		public event Action<Event> Updated;
		#endregion

		public ItemInventory Initialize(ItemStore itemStore)
		{
			if (isInitialized) return this;
			
			isInitialized = true;
			this.itemStore = itemStore;
			Stacks = stacks.AsReadOnly();

			return this;
		}

		/// <summary>
		/// Modify this inventory.
		/// </summary>
		/// <remarks>
		/// This method will not trigger an update on its own. If an update is required the triggerUpdate out will be
		/// non-null.
		/// </remarks>
		/// <param name="modifications"></param>
		/// <param name="clamped">Negative numbers for underflow, positive for overflow</param>
		/// <param name="triggerUpdate"></param>
		/// <returns>True if any modifications were made or any clamping occured</returns>
		public bool Modify( // TODO: MAKE PROTECTED
			(Item Item, int Count)[] modifications,
			out (Item Item, int Count)[] clamped,
			out Action<DateTime> triggerUpdate
		)
		{
			var distinctModifications = new Dictionary<ulong, (Item Item, int Count, int? ExistingCount)>();
			foreach (var modification in modifications)
			{
				if (distinctModifications.TryGetValue(modification.Item.Id, out var entry))
				{
					entry.Count += modification.Count;
					distinctModifications[modification.Item.Id] = entry;
				}
				else
				{
					int? existingCount;
					try { existingCount = Stacks.First(stack => stack.Is(modification.Item)).Count; }
					catch (InvalidOperationException) { existingCount = null; }
					
					distinctModifications[modification.Item.Id] = (
						modification.Item,
						modification.Count,
						existingCount
					);
				}
			}

			var clampedList = new List<(Item Item, int Count)>();
			var eventUpdates = Event.Types.None;
			var events = new Dictionary<ulong, (int OldCount, int NewCount, int DeltaCount, Event.Types Type)>();
			
			foreach (var modification in distinctModifications)
			{
				var existingCount = modification.Value.ExistingCount ?? 0;
				var modificationResult = existingCount + modification.Value.Count;

				if (modificationResult < 0)
				{
					clampedList.Add((modification.Value.Item, modificationResult));
					modificationResult = 0;
				}
				else
				{
					if (TryGetOverflow(modification.Value.Item, modificationResult, out var overflowClampedResult))
					{
						if (overflowClampedResult < 1) Debug.LogError($"Unexpected overflow less than or equal to zero: {overflowClampedResult}");
						clampedList.Add((modification.Value.Item, modificationResult - overflowClampedResult));
						modificationResult = overflowClampedResult;
					}
				}
				
				if (existingCount == modificationResult) continue;

				var delta = modificationResult - existingCount;

				var modificationEvent = delta < 0 ? Event.Types.Subtraction : Event.Types.Addition;
				
				events[modification.Value.Item.Id] = (
					existingCount,
					modificationResult,
					delta,
					modificationEvent
				);

				eventUpdates |= modificationEvent;
			}

			foreach (var modificationEvent in events)
			{
				int? indexToRemove = null;
				var found = false;
				for (var i = 0; i < stacks.Count; i++)
				{
					if (stacks[i].Is(modificationEvent.Key))
					{
						if (modificationEvent.Value.NewCount == 0) indexToRemove = i;
						else stacks[i] = stacks[i].NewCount(modificationEvent.Value.NewCount);
						found = true;
						break;
					}
				}

				if (indexToRemove.HasValue) stacks.RemoveAt(indexToRemove.Value);
				else if (!found) stacks.Add(new ItemStack(modificationEvent.Key, modificationEvent.Value.NewCount));
			}

			clamped = clampedList.ToArray();

			if (eventUpdates == Event.Types.None) triggerUpdate = null;
			else
			{
				triggerUpdate = updateTime =>
				{
					TriggerUpdate(
						new Event(
							updateTime,
							eventUpdates,
							events.ToReadonlyDictionary()
						)
					);
				};
			}

			return triggerUpdate != null || clamped.Any();
		}

		protected virtual bool TryGetOverflow(Item item, int count, out int clampedCount)
		{
			clampedCount = count;
			return false;
		}

		/// <summary>
		/// Transfers from this inventory to another. If any clamping occurred, this returns true.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="stacks"></param>
		/// <param name="clamped"></param>
		/// <returns>True if clamping occurs.</returns>
		public bool TransferTo(
			ItemInventory target,
			ItemStack[] stacks,
			out (Item Item, int Count)[] clamped
		)
		{
			var modifications = new Dictionary<ulong, (Item Item, int Count)>();
			foreach (var stack in stacks)
			{
				var item = itemStore.FirstOrDefault(stack.Id);
				if (item == null)
				{
					Debug.LogError($"Unable to find item with Id {stack.Id}");
					continue;
				}

				if (modifications.TryGetValue(item.Id, out var existingValue))
				{
					existingValue.Count += stack.Count;
					modifications[item.Id] = existingValue;
				}
				else modifications[item.Id] = (item, stack.Count);
			}

			if (modifications.None())
			{
				clamped = new (Item Item, int Count)[0];
				return false;
			}

			var selfWithdrawalModified = Modify(
				// Sign flipped since we are subtracting items
				modifications.Values.Select(m => (m.Item, -m.Count)).ToArray(),
				out var selfWithdrawalClamped,
				out var selfWithdrawalTriggerUpdate
			);

			// No update to trigger means no items were removed...
			if (!selfWithdrawalModified || selfWithdrawalTriggerUpdate == null)
			{
				clamped = selfWithdrawalClamped;
				return false;
			}

			foreach (var clampEntry in selfWithdrawalClamped)
			{
				if (-1 < clampEntry.Count)
				{
					Debug.LogError($"Unexpected zero or positive value for clamped entry for item Id {clampEntry.Item.Id}");
					continue;
				}

				if (modifications.TryGetValue(clampEntry.Item.Id, out var existingValue))
				{
					existingValue.Count += clampEntry.Count;
					if (existingValue.Count == 0) modifications.Remove(clampEntry.Item.Id);
					else modifications[clampEntry.Item.Id] = existingValue;
				}
				else Debug.LogError($"Unexpected missing modification of item Id {clampEntry.Item.Id}");
			}

			if (modifications.None())
			{
				// Confusing, but basically if we tried to pull only items we don't have out of this inventory, then
				// all items would get clamped leaving nothing to transfer to the other inventory -- thus no change in
				// either inventories. However, we should have already caught this by checking if
				// selfWithdrawalTriggerUpdate is equal to null.
				Debug.LogError("Unexpected empty modification list, this should not occur");
				clamped = selfWithdrawalClamped;
				return false;
			}

			var targetDepositModifications = modifications.Values.ToArray(); 
			
			var targetDepositModified = target.Modify(
				targetDepositModifications,
				out var targetDepositClamped,
				out var targetDepositTriggerUpdate
			);

			var updateTime = DateTime.Now;
			selfWithdrawalTriggerUpdate(updateTime);

			// No room was available and nothing got clamped...
			if (!targetDepositModified)
			{
				// Confusing, again, but it's not valid to have some modifications sent and no clamping or update
				// trigger returned.
				Debug.LogError("Unexpected lack of deposit modifications or clamping, this should not occur");
				clamped = targetDepositClamped;
				return true;
			}

			// No update trigger means no items were added, but clamping occurred...
			targetDepositTriggerUpdate?.Invoke(updateTime);

			var clampedDictionary = new Dictionary<ulong, (Item Item, int Count)>();

			// Any elements of selfWithdrawalClamped become overflow for this operation, since we didn't have anywhere
			// to pull it from to begin with...
			foreach (var remainingClamps in selfWithdrawalClamped.Concat(targetDepositClamped))
			{
				// Negative clamps from withdrawal count as overflow and clamps from deposit should already be positive... 
				var overflowCount = Mathf.Abs(remainingClamps.Count);
				if (overflowCount == 0)
				{
					Debug.LogError("Unexpected zero value for clamped item with id "+remainingClamps.Item.Id);
					continue;
				}

				if (clampedDictionary.TryGetValue(remainingClamps.Item.Id, out var existingValue))
				{
					existingValue.Count += overflowCount;
					clampedDictionary[remainingClamps.Item.Id] = existingValue;
				}
				else clampedDictionary[remainingClamps.Item.Id] = (remainingClamps.Item, overflowCount);
			}
			
			clamped = clampedDictionary
				.Select(kv => (kv.Value.Item, kv.Value.Count))
				.ToArray();
			
			return clamped.Any();
		}

		public bool TryOperation<T>(
			string key,
			Func<OperationRequest<T>, OperationResult<T>> operation,
			out T result,
			out int count
		)
		{
			result = default;
			count = 0;
			
			foreach (var stack in Stacks)
			{
				var item = itemStore.FirstOrDefault(stack.Id);
				if (item == null)
				{
					Debug.LogError($"Stack contains missing item with id {stack.Id}");
					continue;
				}

				if (item.TryGet<T>(key, out var value))
				{
					count++;
					var operationResult = operation(new OperationRequest<T>(result, value));
					result = operationResult.Result;

					if (operationResult.SkipRemaining) break;
				}
			}

			return 0 < count;
		}

		public bool TryOperation<T>(
			ItemKey<T> key,
			Func<OperationRequest<T>, OperationResult<T>> operation,
			out T result,
			out int count
		) => TryOperation(
			key.Key,
			operation,
			out result,
			out count
		);

		public bool TrySum(string key, out float result) => TryOperation(key, o => o.Continues(o.Result + o.Value), out result, out _);
		public bool TrySum(ItemKey<float> key, out float result) => TrySum(key.Key, out result);

		public bool TrySum(string key, out int result) => TryOperation(key, o => o.Continues(o.Result + o.Value), out result, out _);
		public bool TrySum(ItemKey<float> key, out int result) => TrySum(key.Key, out result);

		public bool TryAllEqual<T>(
			string key,
			T targetValue,
			out bool result
		)
		{
			result = false;
			var anyOperations = false;
			
			if (targetValue is bool boolTargetValue)
			{
				anyOperations = TryOperation<bool>(
					key,
					o => o.Value == boolTargetValue ? o.Continues(o.Value) : o.Skips(o.Value),
					out var boolResult,
					out _
				);

				if (anyOperations) result = boolResult == boolTargetValue;
			}
			else if (targetValue is int intTargetValue)
			{
				anyOperations = TryOperation<int>(
					key,
					o => o.Value == intTargetValue ? o.Continues(o.Value) : o.Skips(o.Value),
					out var intResult,
					out _
				);
				
				if (anyOperations) result = intResult == intTargetValue;
			}
			else if (targetValue is float floatTargetValue)
			{
				anyOperations = TryOperation<float>(
					key,
					o => Mathf.Approximately(o.Value, floatTargetValue) ? o.Continues(o.Value) : o.Skips(o.Value),
					out var floatResult,
					out _
				);

				if (anyOperations) result = Mathf.Approximately(floatResult, floatTargetValue);
			}
			else if (targetValue is string stringTargetValue)
			{
				anyOperations = TryOperation<string>(
					key,
					o => o.Value == stringTargetValue ? o.Continues(o.Value) : o.Skips(o.Value),
					out var stringResult,
					out _
				);

				if (anyOperations) result = stringTargetValue == stringResult;
			}
			else Debug.LogError($"Unrecognized target type: {typeof(T).Name}");

			return anyOperations;
		}

		public bool TryAnyEqual<T>(
			string key,
			T targetValue,
			out bool result
		)
		{
			result = false;
			var anyOperations = false;
			
			if (targetValue is bool boolTargetValue)
			{
				anyOperations = TryOperation<bool>(
					key,
					o => o.Value == boolTargetValue ? o.Skips(o.Value) : o.Continues(o.Value),
					out var boolResult,
					out _
				);

				if (anyOperations) result = boolResult == boolTargetValue;
			}
			else if (targetValue is int intTargetValue)
			{
				anyOperations = TryOperation<int>(
					key,
					o => o.Value == intTargetValue ? o.Skips(o.Value) : o.Continues(o.Value),
					out var intResult,
					out _
				);
				
				if (anyOperations) result = intResult == intTargetValue;
			}
			else if (targetValue is float floatTargetValue)
			{
				anyOperations = TryOperation<float>(
					key,
					o => Mathf.Approximately(o.Value, floatTargetValue) ? o.Skips(o.Value) : o.Continues(o.Value),
					out var floatResult,
					out _
				);

				if (anyOperations) result = Mathf.Approximately(floatResult, floatTargetValue);
			}
			else if (targetValue is string stringTargetValue)
			{
				anyOperations = TryOperation<string>(
					key,
					o => o.Value == stringTargetValue ? o.Skips(o.Value) : o.Continues(o.Value),
					out var stringResult,
					out _
				);

				if (anyOperations) result = stringTargetValue == stringResult;
			}
			else Debug.LogError($"Unrecognized target type: {typeof(T).Name}");

			return anyOperations;
		}
		
		void TriggerUpdate(Event inventoryEvent)
		{
			lastUpdated = inventoryEvent.UpdateTime;
			
			Updated?.Invoke(inventoryEvent);
		}

		public override string ToString() => ToString(Formats.IncludeItems);

		public string ToString(Formats format)
		{
			var result = $"Item Inventory Contains {Stacks.Count} Stacks | {(isInitialized ? "Initialized" : "Not Initialized")} | {lastUpdated}";

			if (format == Formats.Default) return result;

			var stackFormat = format.HasFlag(Formats.IncludeItemProperties) ? Item.Formats.IncludeProperties | Item.Formats.ExtraPropertyIndent : Item.Formats.Default;
			
			foreach (var stack in Stacks) result += $"\n\t{stack.ToString(itemStore, stackFormat)}";

			return result;
		}
	}
} 