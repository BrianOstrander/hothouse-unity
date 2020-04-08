using UnityEditor;
using UnityEngine;

namespace LunraGames.SubLight
{
	[CustomEditor(typeof(View), true)]
	public class ViewEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			if (DefaultViews.Instance == null) OnNoDefaultViews();
			else OnDefaultViews();
			GUILayout.Space(16f);
			base.OnInspectorGUI();
		}

		void OnNoDefaultViews()
		{
			EditorGUILayout.HelpBox("The scriptable object for default views cannot be found.", MessageType.Error);
		}

		void OnDefaultViews()
		{
			var prefabObject = PrefabUtility.GetCorrespondingObjectFromSource(target);

			if (prefabObject == null && PrefabUtility.GetPrefabInstanceHandle(target) != null) prefabObject = target;

			if (prefabObject == null)
			{
				EditorGUILayout.HelpBox("This view must be a prefab before it can be added to the list of default views.", MessageType.Warning);
				return;
			}

			var isListed = DefaultViews.Instance.Contains(prefabObject);

			if (isListed) GUILayout.Label("This view is already a default view");
			else EditorGUILayout.HelpBox("Adding this view to the list of default views makes it accessable at runtime.", MessageType.Warning);

			//var targetGameObject = (target as View).gameObject;

			GUILayout.BeginHorizontal();
			{
				GUI.enabled = !isListed;
				if (GUILayout.Button("Add", EditorStyles.miniButtonLeft, GUILayout.Height(24f), GUILayout.Width(72f))) DefaultViews.Instance.Add(prefabObject);
				GUI.enabled = isListed;
				if (GUILayout.Button("Remove", EditorStyles.miniButtonRight, GUILayout.Height(24f), GUILayout.Width(72f))) DefaultViews.Instance.Remove(prefabObject);

				GUI.enabled = true;
				if (GUILayout.Button("See all default views", EditorStyles.miniButton, GUILayout.Height(24f))) Selection.activeObject = DefaultViews.Instance;

				// Can't seem to get this to work...
				//GUI.enabled = PrefabUtility.HasPrefabInstanceAnyOverrides(targetGameObject, false);
				//if (GUILayout.Button("Apply Prefab Overrides", EditorStyles.miniButton, GUILayout.Height(24f))) PrefabUtility.ApplyPrefabInstance(targetGameObject, InteractionMode.UserAction);
			}
			GUILayout.EndHorizontal();
		}
	}
}