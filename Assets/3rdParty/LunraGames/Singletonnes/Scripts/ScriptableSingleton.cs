using System.IO;
using UnityEngine;

namespace LunraGames.Singletonnes
{
	public abstract class ScriptableSingleton<T> : ScriptableSingletonBase where T : UnityEngine.Object 
	{
		static T instance;
		public static T Instance => instance ? instance : (instance = Resources.Load<T>(Path.Combine(ContainingDirectory,typeof(T).Name)));
	}
}