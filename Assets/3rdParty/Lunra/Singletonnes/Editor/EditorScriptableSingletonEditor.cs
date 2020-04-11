using System.IO;
using Lunra.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Lunra.Editor.Singletonnes
{
	[CustomEditor(typeof(EditorScriptableSingletonBase), true)]
	public class EditorScriptableSingletonEditor : UnityEditor.Editor 
	{
		const float ButtonHeight = 40f;
		static string wrongInheritence = "Your scriptable object doesn't inherit from "+typeof(EditorScriptableSingleton<>).Name;
		static string wrongNameMessage = "Your asset's name does not match its type.";
		static string wrongPathMessage = "Your asset is not in a valid directory.";
		static string wrongNameAndPathMessage = "Your asset's name does not match its type, and is not in a valid directory.";

		static string autoFixText = "Auto Fix";
		static string defineDirectoryFixText = "Define Specific Editor Folder";
			
		public override void OnInspectorGUI() 
		{
			var typedTarget = (EditorScriptableSingletonBase)target;

			var invalidInheritence = typedTarget.CurrentType == null || typedTarget.CurrentType != typedTarget.GetType();

			if (invalidInheritence)
			{
				EditorGUILayout.HelpBox(wrongInheritence, MessageType.Error);
				DrawDefaultInspector();
				return;
			}

			var path = AssetDatabase.GetAssetPath(typedTarget);
			var assetName = Path.GetFileNameWithoutExtension(path);
			var requiredName = typedTarget.CurrentType.Name;
			var invalidName = assetName != requiredName;
			var invalidPath = path != null && !path.Contains("/Editor/");

			if (invalidName || invalidPath) 
			{
				EditorGUILayout.BeginHorizontal();
				{
					var helpMessage = invalidName && invalidPath ? wrongNameAndPathMessage : (invalidName ? wrongNameMessage : wrongPathMessage);
					EditorGUILayout.HelpBox(helpMessage, MessageType.Error);
					if (invalidPath) 
					{
						if (GUILayout.Button(defineDirectoryFixText, EditorStyles.miniButton, GUILayout.Height(ButtonHeight))) 
						{
							var selectedPath = EditorUtility.SaveFolderPanel("Select a Editor Directory", "Assets", string.Empty);
							if (!string.IsNullOrEmpty(selectedPath)) 
							{
								selectedPath = selectedPath.Substring(Path.GetDirectoryName(Application.dataPath).Length + 1);

								if (selectedPath.EndsWith("/Editor") || selectedPath.Contains("/Editor/")) 
								{
									MoveAsset(path, Path.Combine(selectedPath, requiredName + ".asset")); 
								} 
								else EditorUtilityExtensions.DialogInvalid("You must select an \"Editor\" directory.");
							}
						}
						if (GUILayout.Button(autoFixText, EditorStyles.miniButton, GUILayout.Height(ButtonHeight)))
						{
							MoveAsset(path, Path.Combine(Path.Combine(Path.GetDirectoryName(path), "Editor"), requiredName + ".asset"));
						}
					}
					else
					{
						if (GUILayout.Button(autoFixText, EditorStyles.miniButton, GUILayout.Height(ButtonHeight))) 
						{
							MoveAsset(path, Path.Combine(Path.GetDirectoryName(path), requiredName+".asset"));
						}
					}
				}
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(4f);
			}

			DrawDefaultInspector();
		}

		void MoveAsset(string originPath, string targetPath) 
		{
			var parentDir = Path.GetDirectoryName(targetPath);

			if (!AssetDatabase.IsValidFolder(parentDir)) 
			{
				AssetDatabase.CreateFolder(Path.GetDirectoryName(parentDir), "Editor");
			}

			var moveResult = AssetDatabase.MoveAsset(originPath, targetPath);

			if (!string.IsNullOrEmpty(moveResult)) EditorUtility.DisplayDialog("Error", moveResult, Strings.Dialogs.Responses.Okay);
		}
	}
}