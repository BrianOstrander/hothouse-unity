using System.Linq;

using UnityEditor;
using UnityEngine;

using LunraGamesEditor;
using LunraGamesEditor.Singletonnes;

namespace LunraGames.SubLight
{
	[CustomEditor(typeof(DefaultViews), true)]
	public class DefaultViewsEditor : ScriptableSingletonEditor
	{
		EditorPrefsFloat entryScrolling;
		EditorPrefsBool showDetails;

		bool isDeleting;

		public DefaultViewsEditor()
		{
			var prefix = "LG_SL_DefaultViewsEditor_";

			entryScrolling = new EditorPrefsFloat(prefix + "EntryScrolling");
			showDetails = new EditorPrefsBool(prefix + "ShowDetails");
		}

		void OnEnable()
		{
			isDeleting = false;
		}

		protected override void OnInspectorGUIExtended()
		{
			var typedTarget = target as DefaultViews;

			DrawDefaultInspector();
			GUILayout.Space(16f);

			EditorGUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();
				GUILayout.Label(typedTarget.Entries.Count+" entries in total", EditorStyles.boldLabel);

			}
			EditorGUILayout.EndHorizontal();

			var errorCount = 0;
			foreach (var entry in typedTarget.Entries)
			{
				var currError = string.Empty;
				if (HasError(entry, out currError)) errorCount++;
			}

			if (0 < errorCount) EditorGUILayout.HelpBox("There are " + errorCount + " issue(s) found with the default views", MessageType.Error);

			var wasDeleting = isDeleting;
			EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
			{
				showDetails.Value = EditorGUILayout.ToggleLeft("Show Details", showDetails.Value);

				GUILayout.FlexibleSpace();
				GUILayout.Label("Entry Deletion", EditorStyles.boldLabel);
				if (wasDeleting) EditorGUILayoutExtensions.PushColor(Color.red);
				if (GUILayout.Button(isDeleting ? "Enabled" : "Disabled")) isDeleting = !isDeleting;
				if (wasDeleting) EditorGUILayoutExtensions.PopColor();
			}
			EditorGUILayout.EndHorizontal();

			DefaultViews.Entry deleted = null;

			entryScrolling.VerticalScroll = EditorGUILayout.BeginScrollView(entryScrolling.VerticalScroll);
			{
				foreach (var entry in typedTarget.Entries)
				{
					if (entry == null) continue;

					var error = string.Empty;
					var hasError = HasError(entry, out error);
					var entryName = "< null >";
					if (entry.PrefabRoot != null) entryName = string.IsNullOrEmpty(entry.PrefabRoot.name) ? "< empty or null name >" : entry.PrefabRoot.name;

					if (hasError) EditorGUILayoutExtensions.PushColor(Color.red);
					EditorGUILayout.BeginVertical(EditorStyles.helpBox);
					{
						EditorGUILayout.BeginHorizontal();
						{
							if (isDeleting)
							{
								if (EditorGUILayoutExtensions.XButton()) deleted = entry;
							}

							GUILayout.Label(entryName, EditorStyles.boldLabel);

							if (hasError) GUILayout.Label(error, GUILayout.ExpandWidth(false));
						}
						EditorGUILayout.EndHorizontal();

						if (showDetails.Value)
						{
							EditorGUILayout.BeginHorizontal();
							{
								EditorGUILayout.ObjectField(entry.PrefabRoot, typeof(GameObject), false);
							}
							EditorGUILayout.EndHorizontal();
						}
					}
					EditorGUILayout.EndVertical();
					if (hasError) EditorGUILayoutExtensions.PopColor();
				}
			}
			EditorGUILayout.EndScrollView();

			if (deleted != null)
			{
				typedTarget.Entries = typedTarget.Entries.ExceptOne(deleted).ToList();
				Debug.Log("Deleted entry " + (deleted == null ? "< null >" : "with PrefabRoot " + deleted.PrefabRoot));
			}

			EditorUtility.SetDirty(target);
		}

		bool HasError(DefaultViews.Entry entry, out string error)
		{
			error = null;

			if (entry == null)
			{
				error = "Null Entry";
				return true;
			}

			if (entry.PrefabRoot == null)
			{
				error = "Null PrefabRoot";
				return true;
			}

			return false;
		}
	}
}