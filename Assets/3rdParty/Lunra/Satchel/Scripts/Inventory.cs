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
			Overflow = 1 << 1,
			Underflow = 1 << 2
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
		[JsonProperty] public InventoryConstraint Constraint { get; private set; } = InventoryConstraint.Ignored();
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
		}

		public Inventory Initialize(ItemStore itemStore)
		{
			if (IsInitialized) return this;
			
			this.itemStore = itemStore ?? throw new ArgumentNullException(nameof(itemStore));

			if (Id == IdCounter.UndefinedId) Id = itemStore.IdCounter.Next();
			
			IsInitialized = true;
			
			Stacks = stacks.AsReadOnly();

			Constraint.Initialize(itemStore);

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
			Constraint = InventoryConstraint.Ignored();

			Updated = null;
			UpdatedItem = null;
		}

		public ModificationResults Add(params Stack[] additions)
		{
			var result = Add(additions, out _);
			
			if (result.HasFlag(ModificationResults.Overflow)) Debug.LogError("Unhandled overflow can cause item leaks");

			return result;
		}

		public ModificationResults Add(
			Stack[] additions,
			out Stack[] overflow
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(Add));
			
			var result = ModificationResults.None;
			
			var constraintResult = Constraint.Apply(
				Stacks
					.Concat(additions)
					.ToArray(),
				out var modified,
				out overflow
			);

			if (constraintResult.HasFlag(InventoryConstraint.Events.Modified) || !Stacks.ScrambleEqual(modified))
			{
				result |= ModificationResults.Modified;
				
				var stackEvents = new Dictionary<long, (int OldCount, int NewCount, int DeltaCount, Event.Types Type)>();
				var oldStacks = Stacks.ToArray();
				stacks.Clear();
				
				foreach (var modification in modified)
				{
					var oldCount = oldStacks.FirstOrDefault(s => s.Is(modification.Id)).Count;

					stacks.Add(new Stack(modification.Id, modification.Count));
					
					if (modification.Count == oldCount) continue;
					
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
			}

			if (constraintResult.HasFlag(InventoryConstraint.Events.Overflow)) result |= ModificationResults.Overflow;

			return result;
		}
		
		public ModificationResults Remove(params Stack[] removals)
		{
			var result = Remove(removals, out _);
			
			if (result.HasFlag(ModificationResults.Underflow)) Debug.LogError("Unhandled underflow may cause unexpected problems");

			return result;
		}
		
		public ModificationResults Remove(
			Stack[] removals,
			out Stack[] underflow
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(Remove));
			
			var result = ModificationResults.None;
			
			Dictionary<long, (int Count, int RemovedCount, int Underflow)> consolidated = Stacks
				.ToDictionary(
					stack => stack.Id,
					stack => (stack.Count, 0, 0)
				);

			var anyRemovals = false;
			
			foreach (var stack in removals)
			{
				if (stack.Count == 0) continue;
				
				if (consolidated.TryGetValue(stack.Id, out var entry))
				{
					if (stack.Count <= entry.Count)
					{
						entry.Count -= stack.Count;
						entry.RemovedCount += stack.Count;
					}
					else
					{
						var countUnderflow = Mathf.Abs(entry.Count - stack.Count);
						var countRemoved = stack.Count - countUnderflow;
						entry.Count -= countRemoved;
						entry.RemovedCount += countRemoved;
						entry.Underflow += countUnderflow;
					}

					anyRemovals = true;
				}
				else
				{
					entry = (0, 0, stack.Count);
				}

				consolidated[stack.Id] = entry;
			}

			var underflowList = new List<Stack>();

			void updateUnderflow(
				long key,
				int underflowCount
			)
			{
				if (0 < underflowCount) underflowList.Add(new Stack(key, underflowCount));
			}
			
			if (anyRemovals)
			{
				result |= ModificationResults.Modified;
				
				stacks.Clear();
				var stackEvents = new Dictionary<long, (int OldCount, int NewCount, int DeltaCount, Event.Types Type)>();
				foreach (var kv in consolidated)
				{
					if (0 < kv.Value.Count) stacks.Add(new Stack(kv.Key, kv.Value.Count));
					if (0 < kv.Value.RemovedCount) stackEvents.Add(kv.Key, (kv.Value.Count + kv.Value.RemovedCount, kv.Value.Count, -kv.Value.RemovedCount, Event.Types.Subtraction));
					updateUnderflow(kv.Key, kv.Value.Underflow);
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

			underflow = underflowList.ToArray();
		
			if (underflow.Any()) result |= ModificationResults.Underflow;

			return result;
		}
		
		public Stack New(
			int count,
			out Item item,
			params PropertyKeyValue[] propertyKeyValues
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(New));
			if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Cannot be less than 1");
			
			var result = NewNonDestructive(
				count,
				out item,
				out var additionsStack,
				out var overflowStack,
				propertyKeyValues
			);

			if (result.HasFlag(ModificationResults.Overflow)) itemStore.Destroy(overflowStack);

			return additionsStack;
		}
		
		public ModificationResults NewNonDestructive(
			int count,
			out Item item,
			out Stack additions,
			out Stack overflow,
			params PropertyKeyValue[] propertyKeyValues
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(CloneNonDestructive));
			if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Cannot be less than 1");
			
			item = itemStore.Define(
				i =>
				{
					i.Set(propertyKeyValues);
					i.Set(Constants.InstanceCount, count);
					i.Set(Constants.InventoryId, Id);
				}
			);

			return OnNew(
				count,
				item,
				out additions,
				out overflow
			);
		}
		
		public Stack Clone(
			int count,
			Item reference,
			out Item item,
			params PropertyKeyValue[] propertyKeyValues
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(Clone));
			if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Cannot be less than 1");
			if (reference == null) throw new ArgumentNullException(nameof(reference));

			var result = CloneNonDestructive(
				count,
				reference,
				out item,
				out var additionsStack,
				out var overflowStack,
				propertyKeyValues
			);
			
			if (result.HasFlag(ModificationResults.Overflow)) itemStore.Destroy(overflowStack);

			return additionsStack;
		}

		public ModificationResults CloneNonDestructive(
			int count,
			Item reference,
			out Item item,
			out Stack additions,
			out Stack overflow,
			params PropertyKeyValue[] propertyKeyValues
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(CloneNonDestructive));
			if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Cannot be less than 1");
			if (reference == null) throw new ArgumentNullException(nameof(reference));
			
			item = itemStore.Define(
				reference,
				i =>
				{
					i.Set(propertyKeyValues);
					i.Set(Constants.InstanceCount, count);
					i.Set(Constants.InventoryId, Id);
				}
			);
			
			return OnNew(
				count,
				item,
				out additions,
				out overflow
			);
		}
		
		ModificationResults OnNew(
			int count,
			Item item,
			out Stack additions,
			out Stack overflow
		)
		{
			additions = item.StackOf(count);
			
			var result = Add(
				additions.WrapInArray(),
				out var overflowArray
			);

			if (result.HasFlag(ModificationResults.Overflow))
			{
				if (overflowArray.Length != 1) throw new Exception("More than one overflow stack was returned");
				overflow = overflowArray.First();
				if (overflow.IsNot(item)) throw new Exception($"Expected overflow of item {item} but got an item of id {overflow.Id} instead");

				additions -= overflow.Count;
			}
			else overflow = item.StackOfZero();

			return result;
		}

		public ModificationResults DestroyAll() => Destroy(Stacks.ToArray(), out _);

		public ModificationResults Destroy(params Item[] destroyed)
		{
			var destroyedIds = destroyed
				.Select(i => i.Id)
				.Distinct()
				.ToArray();

			if (destroyedIds.None()) return ModificationResults.None;
			
			var destroyedStacks = Stacks
				.Where(s => destroyedIds.Contains(s.Id))
				.ToArray();

			return destroyedStacks.None() ? ModificationResults.None : Destroy(destroyedStacks);
		}
		
		public ModificationResults Destroy(params Stack[] destroyed)
		{
			var result = Destroy(destroyed, out _);
			
			if (result.HasFlag(ModificationResults.Underflow)) Debug.LogError("Unhandled underflow on destruction may cause unexpected problems");
			
			return result;
		}
		
		public ModificationResults Destroy(
			Stack[] destroyed,
			out Stack[] underflow
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(DestroyAll));

			if (destroyed.None())
			{
				underflow = new Stack[0];
				return ModificationResults.None;
			}
			
			var result = Remove(
				destroyed,
				out underflow
			);

			if (!result.HasFlag(ModificationResults.Modified))
			{
				// None of the specified stacks were present...
				return result;
			}

			if (!result.HasFlag(ModificationResults.Underflow))
			{
				// All of the specified stacks were present and removed...
				itemStore.Destroy(destroyed);
				return result;
			}
			
			// Only some of the specified stacks were present and removed, some overflowed...
			var consolidated = destroyed.ResolveToDictionary(
				s => s.Id,
				s => s.Count,
				r => r.ExistingValue + r.DuplicateValue
			);

			// Underflow is removed, since it never existed and should not get destroyed...
			foreach (var stack in underflow) consolidated[stack.Id] -= stack.Count;

			itemStore.Destroy(
				consolidated
					.Select(c => new Stack(c.Key, c.Value))
					.ToArray()
			);

			return result;
		}

		public ItemBuilder Build()
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(Build));
			return new ItemBuilder(itemStore, this);
		}

		public bool UpdateConstraint(
			InventoryConstraint constraint
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(UpdateConstraint));
			
			var result = UpdateConstraintNonDestructive(
				constraint,
				out var overflow
			);

			if (result.HasFlag(ModificationResults.Overflow)) itemStore.Destroy(overflow);

			return result.HasFlag(ModificationResults.Modified);
		}
		
		public ModificationResults UpdateConstraintNonDestructive(
			InventoryConstraint constraint,
			out Stack[] overflow
		)
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(UpdateConstraintNonDestructive));
			
			Constraint = constraint;

			var constraintResult = Constraint.Apply(
				Stacks.ToArray(),
				out _,
				out overflow
			);

			var result = ModificationResults.None;
			
			if (constraintResult.HasFlag(InventoryConstraint.Events.Modified))
			{
				result |= ModificationResults.Modified | ModificationResults.Overflow;
				
				var removeResult = Remove(overflow, out _);
				if (removeResult != ModificationResults.Modified) Debug.LogError($"Unexpected remove result: {removeResult:F}");
			}

			return result;
		}

		public bool Clear()
		{
			if (!IsInitialized) throw new NonInitializedInventoryOperationException(nameof(Clear));

			if (Stacks.None()) return false;
			
			var destroyed = Stacks.ToArray();
			Remove(destroyed, out _);

			return itemStore.Destroy(destroyed);
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

		public override string ToString() => ToString(Formats.IncludeItems | Formats.IncludeItemProperties);

		public string ToString(Formats format)
		{
			var result = $"Item Inventory Contains {Stacks.Count} Stacks | {(IsInitialized ? "Initialized" : "Not Initialized")} | {lastUpdated}";

			if (format == Formats.Default) return result;

			var stackFormat = format.HasFlag(Formats.IncludeItemProperties) ? Item.Formats.IncludeProperties | Item.Formats.ExtraPropertyIndent : Item.Formats.Default;
			
			foreach (var stack in Stacks) result += $"\n\t{stack.ToString(itemStore, stackFormat)}";

			result += "\n" + Constraint;
			
			return result;
		}
	}
} 