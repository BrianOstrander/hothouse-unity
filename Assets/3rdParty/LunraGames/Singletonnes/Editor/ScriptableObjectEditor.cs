using System.IO;
using UnityEditor;
using UnityEngine;
using LunraGames.Singletonnes;

namespace LunraGamesEditor.Singletonnes
{
	[CustomEditor(typeof(ScriptableSingletonBase), true)]
	public class ScriptableSingletonEditor : Editor 
	{
		static float ButtonHeight = 40f;
		static string WrongNameMessage = "Your asset's name does not match its type.";
		static string WrongPathMessage = "Your asset is not in a valid directory.";
		static string WrongNameAndPathMessage = "Your asset's name does not match its type, and is not in a valid directory.";

		static string AutoFixText = "Auto Fix";
		static string DefineDirectoryFixText = "Define Specific Resources Folder";
			
		public override void OnInspectorGUI() 
		{
			var typedTarget = (ScriptableSingletonBase)target;

			var path = AssetDatabase.GetAssetPath(typedTarget);
			var assetName = Path.GetFileNameWithoutExtension(path);
			var requiredName = typedTarget.CurrentType.Name;
			var requiredPathEnding = Path.Combine("Resources", Path.Combine(ScriptableSingletonBase.ContainingDirectory, requiredName));
			var invalidName = assetName != requiredName;
			var invalidPath = !path.EndsWith(Path.Combine(Path.Combine("Resources", ScriptableSingletonBase.ContainingDirectory), Path.GetFileName(path)));

			if (invalidName || invalidPath) 
			{
				EditorGUILayout.BeginHorizontal();
				{
					var helpMessage = invalidName && invalidPath ? WrongNameAndPathMessage : (invalidName ? WrongNameMessage : WrongPathMessage);
					EditorGUILayout.HelpBox(helpMessage, MessageType.Error);
					if (invalidPath) 
					{
						if (GUILayout.Button(DefineDirectoryFixText, EditorStyles.miniButton, GUILayout.Height(ButtonHeight))) 
						{
							var selectedPath = EditorUtility.SaveFolderPanel("Select a Resources Directory", "Assets", string.Empty);
							if (!string.IsNullOrEmpty(selectedPath)) 
							{
								selectedPath = selectedPath.Substring(Path.GetDirectoryName(Application.dataPath).Length + 1);

								if (selectedPath.EndsWith("Resources")) 
								{
									MoveAsset(path, Path.Combine(selectedPath, Path.Combine(ScriptableSingletonBase.ContainingDirectory, requiredName + ".asset")));
								}
								else if (selectedPath.EndsWith(Path.Combine("Resources", ScriptableSingletonBase.ContainingDirectory))) 
								{
									MoveAsset(path, Path.Combine(selectedPath, requiredName + ".asset"));
								} 
								else EditorUtilityExtensions.DialogInvalid("You must select a \"Resources\" directory.");
							}
						}
						if (GUILayout.Button(AutoFixText, EditorStyles.miniButton, GUILayout.Height(ButtonHeight)))
						{
							MoveAsset(path, Path.Combine("Assets", requiredPathEnding + ".asset"));
						}
					}
					else if (invalidName)
					{
						if (GUILayout.Button(AutoFixText, EditorStyles.miniButton, GUILayout.Height(ButtonHeight))) 
						{
							MoveAsset(path, Path.Combine(Path.GetDirectoryName(path), requiredName+".asset"));
						}
					}
				}
				EditorGUILayout.EndHorizontal();

				GUILayout.Space(4f);
			}

			OnInspectorGUIExtended();
		}

		protected virtual void OnInspectorGUIExtended()
		{
            DrawDefaultInspector();
		}

		void MoveAsset(string originPath, string targetPath) 
		{
			var moveResult = string.Empty;
			var parentDir = Path.GetDirectoryName(targetPath);

			if (!AssetDatabase.IsValidFolder(parentDir)) 
			{
				var resourceDir = Path.GetDirectoryName(parentDir);
				if (!AssetDatabase.IsValidFolder(resourceDir)) AssetDatabase.CreateFolder(Path.GetDirectoryName(resourceDir), "Resources");
				AssetDatabase.CreateFolder(resourceDir, ScriptableSingletonBase.ContainingDirectory);
			}

			moveResult = AssetDatabase.MoveAsset(originPath, targetPath);

			if (!string.IsNullOrEmpty(moveResult)) EditorUtilityExtensions.DialogError(moveResult);
		}
	}
}