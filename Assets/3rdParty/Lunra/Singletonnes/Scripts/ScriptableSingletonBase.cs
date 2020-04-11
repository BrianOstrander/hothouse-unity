using System;
using System.IO;
using UnityEngine;

namespace Lunra.Singletonnes
{
	public abstract class ScriptableSingletonBase : ScriptableObject
	{
		public static string ContainingDirectory = "ScriptableSingletons";

		public Type CurrentType { get; }

		protected ScriptableSingletonBase()
		{
			CurrentType = GetType();
		}
	}
}