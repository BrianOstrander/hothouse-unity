using System;
using System.IO;
using UnityEngine;

namespace LunraGames.Singletonnes
{
	public abstract class ScriptableSingleton<T> : ScriptableSingletonBase where T : UnityEngine.Object 
	{
		static T _Instance;
		public static T Instance 
		{
			get 
			{
				return _Instance ?? (_Instance = Resources.Load<T>(Path.Combine(ContainingDirectory,typeof(T).Name)));
			}
		}
	}
}