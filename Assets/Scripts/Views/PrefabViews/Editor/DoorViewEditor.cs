using Lunra.Editor.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Services.Editor;
using Lunra.StyxMvp.Models;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Views.Editor
{
	[CustomEditor(typeof(DoorView))]
	public class DoorViewEditor : UnityEditor.Editor
	{
		void OnSceneGUI()
		{
			var typedTarget = target as DoorView;
			if (typedTarget == null) return;

			var isGamePlaying = GameStateEditorUtility.GetGame(out var game);

			var modelIdNullOrEmpty = string.IsNullOrEmpty(typedTarget.ModelId);
			var model = modelIdNullOrEmpty ? null : game?.Doors.FirstOrDefaultActive(typedTarget.ModelId);
			
			Handles.BeginGUI();
			{
				if (isGamePlaying && model == null)
				{
					GUIExtensions.PushColor(Color.red);
					{
						GUILayout.Label(
							modelIdNullOrEmpty ? "Null or empty ModelId" : "Cannot find DoorModel with id: "+Model.ShortenId(typedTarget.ModelId)
						);
					}
					GUIExtensions.PopColor();
				}
				
				GUIExtensions.PushEnabled(isGamePlaying && model != null);
				{
					if (GUILayout.Button("Open Door", GUILayout.ExpandWidth(false)))
					{
						model.IsOpen.Value = true;
					}
				}
				GUIExtensions.PopEnabled();
			}
			Handles.EndGUI();
		}
	}
}