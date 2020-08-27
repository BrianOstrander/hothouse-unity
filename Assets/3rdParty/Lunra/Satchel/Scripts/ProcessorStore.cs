using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Core;
using UnityEngine;

namespace Lunra.Satchel
{
	public class ProcessorStore
	{
		ItemStore itemStore;
		List<Processor> processors = new List<Processor>();
		
		public ProcessorStore Initialize(
			ItemStore itemStore
		)
		{
			this.itemStore = itemStore;

			foreach (var processorType in ReflectionUtility.GetTypesWithAttribute<ProcessorAttribute, Processor>(true))
			{
				if (ReflectionUtility.TryGetInstanceOfType<Processor>(processorType, out var processorInstance))
				{
					try { processors.Add(processorInstance.Initialize(itemStore)); }
					catch (Exception e) { Debug.LogException(e); }
				}
			}

			// Order them by name as well so there is some consistency to how similar priority processors are ordered.
			processors = processors
				.OrderBy(p => p.Priority)
				.ThenBy(p => p.GetType().Name)
				.ToList();

			return this;
		}

		public void Process()
		{
			itemStore.Iterate(OnProcess);
		}

		void OnProcess(Item item)
		{
			if (item.Get(Constants.Destroyed)) return;

			foreach (var processor in processors)
			{
				try
				{
					try
					{
						if (!processor.IsValid(item)) continue;
					}
					catch (Exception e)
					{
						throw new Exception($"Processor {processor.GetType().Name} encountered an exception on {nameof(processor.IsValid)} for item {item}", e);
						continue;
					}

					try
					{
						processor.Process(item);
					}
					catch (Exception e)
					{
						throw new Exception($"Processor {processor.GetType().Name} encountered an exception on {nameof(processor.Process)} for item {item}", e);
					}
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}
		}
	}
}