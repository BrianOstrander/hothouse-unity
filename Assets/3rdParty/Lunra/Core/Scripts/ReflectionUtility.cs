using System;
using System.Collections.Generic;
using System.Linq;
using Lunra.Hothouse.Models;
using UnityEngine;

namespace Lunra.Core
{
	public static class ReflectionUtility
	{
		public static Type[] GetTypesWithAttribute<A, T>(bool suppressWarnings = false)
		{
			var results = new List<Type>();

			foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()))
			{
				if (type.IsAbstract) continue;
				if (type.GetCustomAttributes(typeof(A), true).None()) continue;
				if (!typeof(T).IsAssignableFrom(type))
				{
					if (!suppressWarnings) Debug.LogWarning("The class \"" + type.FullName + "\" tries to include the \"" + typeof(A).Name + "\" attribute without inheriting from the \"" + typeof(T).FullName + "\" class");
					continue;
				}
				
				results.Add(type);
			}

			return results.ToArray();
		}
		
		public static bool TryGetInstanceOfType<T>(
			Type type,
			out T instance
		)
		{
			instance = default;
			
			try
			{
				instance = (T) Activator.CreateInstance(type);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				return false;
			}

			return true;
		}
	}
}