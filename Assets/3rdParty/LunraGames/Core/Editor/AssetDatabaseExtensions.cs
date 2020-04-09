using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using Object = UnityEngine.Object;

namespace LunraGamesEditor
{
	public static class AssetDatabaseExtensions
	{
		public static string GetAbsoluteAssetPath(Object assetObject)
		{
			return Path.Combine(Project.Root.FullName, AssetDatabase.GetAssetPath(assetObject));
		}

		public static string GetRelativeAssetPath(string absoluteAssetPath)
		{
			if (string.IsNullOrEmpty(absoluteAssetPath)) throw new ArgumentException("Cannot be null or empty");
			if (!absoluteAssetPath.StartsWith(Project.Root.FullName)) throw new ArgumentException("Path must be inside project");
			return absoluteAssetPath.Remove(0, Project.Root.FullName.Length + 1);
		}

		/// <summary>
		/// Creates a new ScriptableObject of the specified type with the name provided and selects it upon creation.
		/// If the directory is invalid it will show a warning.
		/// </summary>
		/// <param name="name">Name.</param>
		/// <typeparam name="T">Type of the ScriptableObject.</typeparam>
		public static T CreateObject<T>(string name)
			where T : ScriptableObject
		{
			var directory = SelectionExtensions.Directory();
			if (directory == null)
			{
				EditorUtilityExtensions.DialogInvalid(Strings.Dialogs.Messages.SelectValidDirectory);
				return null;
			}

			var config = ScriptableObject.CreateInstance<T>();
			AssetDatabase.CreateAsset(config, Path.Combine(directory, name + ".asset"));
			Selection.activeObject = config;
			return config;
		}
	}
}