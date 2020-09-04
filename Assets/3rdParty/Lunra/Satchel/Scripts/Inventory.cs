using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Lunra.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Lunra.Satchel
{
	public class Inventory
	{
		[Flags]
		public enum Formats
		{
			Default = 0,
			IncludeItems = 1 << 0,
			IncludeItemProperties = 1 << 1
		}

		[Flags]
		public enum ModificationResults
		{
			None = 0,
			Modified = 1 << 0,
			Underflow = 1 << 1
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
			[JsonProperty] public bool IsEmpty { get; private set; }
			[JsonProperty] public ReadOnlyDictionary<long, (int OldCount, int NewCount, int DeltaCount, Types Type)> StackEvents { get; private set; }
			
			public Event(
				DateTime updateTime,
				Types updates,
				bool isEmpty,
				ReadOnlyDictionary<long, (int OldCount, int NewCount, int DeltaCount, Types Type)> stackEvents
			)
			{
				UpdateTime = updateTime;
				Updates = updates;
				IsEmpty = isEmpty;
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
		[JsonProperty] List<Stack> stacks = new List<Stack>();
		[JsonProperty] DateTime lastUpdated;

		[JsonProperty] public long Id { get; private set; }
		#endregion

		#region Non Serialized
		[JsonIgnore] public bool IsInitialized { get; private set; }
		ItemStore itemStore;
		[JsonIgnore] public ReadOnlyCollection<Stack> Stacks { get; private set; }
		public event Action<Event> Updated;
		public event Action<ItemStore.Event> UpdatedItem;
		#endregion

		public Inventory(long id)
		{
			if (id == IdCounter.UndefinedId) throw new Exception($"Id {id} is an invalid and undefined value");
			Id = id;
			lastUpdated = DateTime.Now;
		}

		public Inventory Initialize(ItemStore itemStore)
		{
			if (IsInitialized) return this;
			
			this.itemStore = itemStore ?? throw new ArgumentNullException(nameof(itemStore));

			if (Id == IdCounter.UndefinedId) Id = itemStore.IdCounter.Next();
			
			IsInitialized = true;
			
			Stacks = stacks.AsReadOnly();

			itemStore.Register(this, TriggerItemUpdate);

			return this;
		}

		/// <summary>
		/// This is similar to creating a whole new inventory, by destroying any remaining items, changing its id, and
		/// unregistering itself from the item store. It will need to be reinitialized after calling this.
		/// </summary>
		public void Reset()
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(Reset));
			
			DestroyAll();
			itemStore.UnRegister(this);
			
			Id = IdCounter.UndefinedId;
			itemStore = null;
			IsInitialized = false;
			Stacks = null;

			Updated = null;
			UpdatedItem = null;
		}

		/// <summary>
		/// Deposits the specified items into the inventory, it is expected that no instances of them exist in any other
		/// inventories.
		/// </summary>
		/// <param name="requests"></param>
		/// <returns></returns>
		public ModificationResults Deposit(params Stack[] requests)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(Deposit));
			if (requests.None()) return ModificationResults.None;
			
			foreach (var request in requests)
			{
				if (request.Count <= 0) continue;
				if (stacks.Any(s => s.Is(request.Id))) continue;
				stacks.Add(request.NewEmpty());
			}

			return Increment(requests);
		}

		public ModificationResults Increment(params Stack[] requests)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(Increment));
			if (requests.None()) return ModificationResults.None;

			var modifications = Stacks.ToDictionary(
				s => s.Id,
				s => s.Count
			);

			var anyAddition = false;
			
			foreach (var request in requests)
			{
				if (request.Count == 0) continue;

				if (!modifications.TryGetValue(request.Id, out var count))
				{
					Debug.LogError($"Attempted to increment item [ {request.Id} ], but it was not present in this inventory");
					continue;
				}
				
				anyAddition = true;

				modifications[request.Id] = count + request.Count;
			}

			if (!anyAddition) return ModificationResults.None;
			
			var stackEvents = new Dictionary<long, (int OldCount, int NewCount, int DeltaCount, Event.Types Type)>();
			var oldStacks = Stacks.ToArray();
			stacks.Clear();
				
			foreach (var modification in modifications.Select(m => new Stack(m.Key, m.Value)))
			{
				var oldCount = oldStacks.FirstOrDefault(s => s.Is(modification.Id)).Count;

				stacks.Add(modification);
					
				if (modification.Count == oldCount) continue;

				if (itemStore.TryGet(modification.Id, out var modificationItem))
				{
					if (modificationItem.InventoryId == IdCounter.UndefinedId) modificationItem.ForceUpdateInventoryId(Id);
					else if (modificationItem.InventoryId != Id) Debug.LogError($"Adding item [ {modification.Id} ] to inventory [ {Id} ], but item already assigned to inventory [ {modificationItem.InventoryId} ], unexpected behaviour may occur");
					
					modificationItem.ForceUpdateInstanceCount(modification.Count);
				}
				else Debug.LogError($"Could not find item with Id {modification.Id}");
					
				stackEvents.Add(
					modification.Id,
					(
						oldCount,
						modification.Count,
						modification.Count - oldCount,
						Event.Types.Addition
					)
				);
			}
				
			TriggerUpdate(
				new Event(
					DateTime.Now,
					Event.Types.Addition,
					Stacks.None(),
					stackEvents.ToReadonlyDictionary()
				)	
			);

			return ModificationResults.Modified;
		}
		
		
		public Stack[] Withdrawal(
			params Stack[] requests
		)
		{
			var result = Withdrawal(
				requests,
				out var results,
				out _
			);
			
			if (result.HasFlag(ModificationResults.Underflow)) Debug.LogError("Unhandled underflow may cause unexpected problems");

			return results;
		}
		
		public ModificationResults Withdrawal(
			Stack[] requests,
			out Stack[] results,
			out Stack[] underflow
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(Withdrawal));

			var result = OnDecrement(
				requests,
				out var modified,
				out var destroyed,
				out underflow
			);

			var resultsList = new List<Stack>();

			foreach (var entry in modified)
			{
				if (itemStore.TryGet(entry.Id, out var item))
				{
					resultsList.Add(
						itemStore
							.Define(
								item,
								i => i.ForceUpdateInstanceCount(entry.Delta)
							)
							.StackOf(entry.Delta)
					);
				}
				else Debug.LogError($"Unable to find item with id [ {entry.Id} ]");
			}

			foreach (var entry in destroyed)
			{
				if (itemStore.TryGet(entry.Id, out var item))
				{
					item.ForceUpdateInstanceCount(entry.Count);
					
					resultsList.Add(
						item.StackOf(entry.Count)
					);
				}
				else Debug.LogError($"Unable to find item with id [ {entry.Id} ]");
			}

			results = resultsList.ToArray();
			// results = modified
			// 	.Select(m => new Stack(m.Id, m.Delta))
			// 	.Concat(destroyed)
			// 	.ToArray();

			return result;
		}
		
		public ModificationResults Decrement(
			params Stack[] requests
		)
		{
			var result = OnDecrement(
				requests,
				out _,
				out _,
				out _
			);
			
			if (result.HasFlag(ModificationResults.Underflow)) Debug.LogError("Unhandled underflow may cause unexpected problems");

			return result;
		}
		
		ModificationResults OnDecrement(
			Stack[] requests,
			out (long Id, int OldCount, int NewCount, int Delta)[] modified,
			out Stack[] destroyed,
			out Stack[] underflow
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(Decrement));
			
			var result = ModificationResults.None;
			
			Dictionary<long, (int Count, int RemovedCount, int Underflow)> consolidated = Stacks
				.ToDictionary(
					stack => stack.Id,
					stack => (stack.Count, 0, 0)
				);

			var anyModifications = false;
			
			foreach (var request in requests)
			{
				if (request.Count == 0) continue;
				
				if (consolidated.TryGetValue(request.Id, out var entry))
				{
					if (request.Count <= entry.Count)
					{
						entry.Count -= request.Count;
						entry.RemovedCount += request.Count;
					}
					else
					{
						var countUnderflow = Mathf.Abs(entry.Count - request.Count);
						var countRemoved = request.Count - countUnderflow;
						entry.Count -= countRemoved;
						entry.RemovedCount += countRemoved;
						entry.Underflow += countUnderflow;
					}

					anyModifications = true;
				}
				else
				{
					entry = (0, 0, request.Count);
				}

				consolidated[request.Id] = entry;
			}

			var underflowList = new List<Stack>();

			void updateUnderflow(
				long key,
				int underflowCount
			)
			{
				if (0 < underflowCount) underflowList.Add(new Stack(key, underflowCount));
			}
			
			var modifiedList = new List<(long Id, int OldCount, int NewCount, int Delta)>();
			var destroyedList = new List<Stack>();
			
			if (anyModifications)
			{
				result |= ModificationResults.Modified;

				stacks.Clear();
				var stackEvents = new Dictionary<long, (int OldCount, int NewCount, int DeltaCount, Event.Types Type)>();
				foreach (var kv in consolidated)
				{
					if (itemStore.TryGet(kv.Key, out var stackEventItem))
					{
						stackEventItem.ForceUpdateInstanceCount(kv.Value.Count);

						if (stackEventItem.InstanceCount == 0)
						{
							destroyedList.Add(stackEventItem.StackOf(kv.Value.RemovedCount));
							stackEventItem.ForceUpdateInventoryId(IdCounter.UndefinedId);
						}
						else
						{
							stacks.Add(stackEventItem.StackOf(kv.Value.Count));
							if (kv.Value.RemovedCount != 0 || kv.Value.Underflow != 0)
							{
								modifiedList.Add((kv.Key, kv.Value.RemovedCount + kv.Value.Count, kv.Value.Count, kv.Value.RemovedCount));
							}
						}
						
						if (0 < kv.Value.RemovedCount) stackEvents.Add(kv.Key, (kv.Value.Count + kv.Value.RemovedCount, kv.Value.Count, -kv.Value.RemovedCount, Event.Types.Subtraction));
						
						updateUnderflow(kv.Key, kv.Value.Underflow);
					}
					else Debug.LogError($"No item found with id [ {kv.Key} ]");
				}

				TriggerUpdate(
					new Event(
						DateTime.Now,
						Event.Types.Subtraction,
						Stacks.None(),
						stackEvents.ToReadonlyDictionary()
					)
				);
			}
			else
			{
				foreach (var kv in consolidated) updateUnderflow(kv.Key, kv.Value.Underflow);
			}
			
			modified = modifiedList.ToArray();
			destroyed = destroyedList.ToArray();

			underflow = underflowList.ToArray();
		
			if (underflow.Any()) result |= ModificationResults.Underflow;

			return result;
		}

		public Stack New(
			int count,
			params PropertyKeyValue[] propertyKeyValues
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(New));

			return New(count, out _, propertyKeyValues);
		}

		public Stack New(
			int count,
			out Item item,
			params PropertyKeyValue[] propertyKeyValues
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(New));
			if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Cannot be less than 1");
			
			item = itemStore.Define(
				i =>
				{
					i.Set(propertyKeyValues);
					i.ForceUpdateInstanceCount(count);
				}
			);

			return OnNew(
				count,
				item
			);
		}
		
		public Stack New(
			int count,
			Item reference,
			out Item item,
			params PropertyKeyValue[] propertyKeyValues
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(New));
			if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Cannot be less than 1");
			if (reference == null) throw new ArgumentNullException(nameof(reference));
			
			item = itemStore.Define(
				reference,
				i =>
				{
					i.Set(propertyKeyValues);
					i.ForceUpdateInstanceCount(count);
				}
			);
			
			return OnNew(
				count,
				item
			);
		}
		
		Stack OnNew(
			int count,
			Item item
		)
		{
			item.ForceUpdateInventoryId(Id);

			var result = item.StackOf(count);
			
			Deposit(result);

			return result;
		}

		public ModificationResults DestroyAll() => Destroy(Stacks.ToArray(), out _);

		public ModificationResults Destroy(
			params Item[] requests
		)
		{
			var destroyedIds = requests
				.Select(i => i.Id)
				.Distinct()
				.ToArray();

			if (destroyedIds.None()) return ModificationResults.None;
			
			var destroyedStacks = Stacks
				.Where(s => destroyedIds.Contains(s.Id))
				.ToArray();

			return destroyedStacks.None() ? ModificationResults.None : Destroy(destroyedStacks);
		}
		
		public ModificationResults Destroy(
			params Stack[] requests
		)
		{
			var result = Destroy(requests, out _);
			
			if (result.HasFlag(ModificationResults.Underflow)) Debug.LogError("Unhandled underflow on destruction may cause unexpected problems");
			
			return result;
		}
		
		public ModificationResults Destroy(
			Stack[] requests,
			out Stack[] underflow
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(DestroyAll));

			if (requests.None())
			{
				underflow = new Stack[0];
				return ModificationResults.None;
			}

			return OnDecrement(
				requests,
				out _,
				out _,
				out underflow
			);
		}
		
		/// <summary>
		/// For iterating over all items when you don't need to access the actual list.
		/// </summary>
		/// <remarks>
		/// Adding or removing items while iterating here will cause an exception.
		/// </remarks>
		public IEnumerable<(Item Item, Stack Stack)> All()
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(All));
			
			foreach (var stack in stacks)
			{
				if (itemStore.TryGet(stack.Id, out var item)) yield return (item, stack);
				else Debug.LogError($"Unable to find item with Id {stack.Id}");
			}
		}
		
		/// <summary>
		/// For iterating over all items when you don't need to access the actual list.
		/// </summary>
		/// <remarks>
		/// Adding or removing items while iterating here will cause an exception.
		/// </remarks>
		/// <param name="predicate"></param>
		public IEnumerable<(Item Item, Stack Stack)> All(Func<Item, bool> predicate)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(All));

			foreach (var element in All())
			{
				if (predicate(element.Item)) yield return element;
			}
		}

		public bool TryFindFirst(
			Func<Item, bool> predicate,
			out Item item
		)
		{
			return TryFindFirst(predicate, out item, out _);
		}
		
		public bool TryFindFirst(
			Func<Item, bool> predicate,
			out Item item,
			out Stack stack
		)
		{
			foreach (var s in Stacks)
			{
				if (itemStore.TryGet(s.Id, out item))
				{
					if (predicate(item))
					{
						stack = s;
						return true;
					}
					item = null;
				}
				else Debug.LogError($"Unrecognized item id {s.Id}");
			}

			item = null;
			stack = default;
			return false;
		}
		
		public bool TryFindFirst(
			out Item item,
			params PropertyKeyValue[] keyValues
		)
		{
			return TryFindFirst(out item, out _, keyValues);
		}
		
		public bool TryFindFirst(
			out Item item,
			out Stack stack,
			params PropertyKeyValue[] keyValues
		)
		{
			foreach (var s in Stacks)
			{
				if (itemStore.TryGet(s.Id, out item))
				{
					var isMatch = true;
					foreach (var kv in keyValues)
					{
						if (!kv.IsEqualToValueIn(item))
						{
							isMatch = false;
							break;
						}
					}

					if (isMatch)
					{
						stack = s;
						return true;
					}
				}
				else Debug.LogError($"Unrecognized item id {s.Id}");
			}

			item = null;
			stack = default;
			return false;
		}

		public bool TryOperation<T>(
			string key,
			Func<OperationRequest<T>, OperationResult<T>> operation,
			out T result,
			out int count
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(TryOperation));
			
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
			PropertyKey<T> key,
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
		public bool TrySum(PropertyKey<float> key, out float result) => TrySum(key.Key, out result);

		public bool TrySum(string key, out int result) => TryOperation(key, o => o.Continues(o.Result + o.Value), out result, out _);
		public bool TrySum(PropertyKey<float> key, out int result) => TrySum(key.Key, out result);

		public bool TryAllEqual<T>(
			string key,
			T targetValue,
			out bool result
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(TryAllEqual));
			
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
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(TryAnyEqual));
			
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

		void TriggerItemUpdate(ItemStore.Event itemEvent)
		{
			lastUpdated = itemEvent.UpdateTime;
			UpdatedItem?.Invoke(itemEvent);
		}

		public static ModificationResults Transfer(
			Stack[] requests,
			Inventory source,
			Inventory destination
		)
		{
			var result = Transfer(
				requests,
				source,
				destination,
				out _
			);
			
			if (result.HasFlag(ModificationResults.Underflow)) Debug.LogError("Unhandled underflow may cause unexpected problems");

			return result;
		}
		
		public static ModificationResults Transfer(
			Stack[] requests,
			Inventory source,
			Inventory destination,
			out Stack[] underflow
		)
		{
			var withdrawalResult = source.Withdrawal(
				requests,
				out var withdrawalResults,
				out underflow
			);
		
			if (!withdrawalResult.HasFlag(ModificationResults.Modified)) return withdrawalResult;

			return destination.Deposit(withdrawalResults);
		}

		public override string ToString() => ToString(Formats.IncludeItems | Formats.IncludeItemProperties);

		public string ToString(Formats format)
		{
			var result = $"Item Inventory [ {Id} ] Contains {Stacks.Count} Stacks | {(IsInitialized ? "Initialized" : "Not Initialized")} | {lastUpdated}";

			if (format == Formats.Default) return result;

			var stackFormat = format.HasFlag(Formats.IncludeItemProperties) ? Item.Formats.IncludeProperties | Item.Formats.ExtraPropertyIndent : Item.Formats.Default;
			
			foreach (var stack in Stacks) result += $"\n\t{stack.ToString(itemStore, stackFormat)}";

			return result;
		}
	}
} 