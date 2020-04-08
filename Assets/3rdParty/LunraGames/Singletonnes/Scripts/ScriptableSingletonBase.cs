using System;
using System.IO;
using UnityEngine;

namespace LunraGames.Singletonnes
{
	public abstract class ScriptableSingletonBase : ScriptableObject
	{
		public static string ContainingDirectory = "ScriptableSingletons";

		Type _CurrentType;
		public Type CurrentType { get { return _CurrentType; } }

		protected ScriptableSingletonBase()
		{
			_CurrentType = GetType();
		}
	}
}