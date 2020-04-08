using UnityEngine;
using UnityEditor;

namespace LunraGamesEditor.Singletonnes
{
	public abstract class EditorScriptableSingleton<T> : EditorScriptableSingletonBase 
		where T : EditorScriptableSingleton<T>
	{
		static T _Instance;

		public static T Instance { get { return _Instance ?? ( _Instance = FindInstance()); } }

		static T FindInstance()
		{
			var instances = AssetDatabase.FindAssets("t:ScriptableObject "+typeof(T).Name);

			if (instances.Length != 1) 
			{
				Debug.LogError(instances.Length == 0 ? "No instance of Noise Maker settings exist" : "More than one instance of Noise Maker settings exists");
				return null;
			}
			return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(instances[0]));
		}

		protected EditorScriptableSingleton() : base(typeof(T)) {}

	}
}