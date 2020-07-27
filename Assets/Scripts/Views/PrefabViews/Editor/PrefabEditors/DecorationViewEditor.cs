using System;
using System.Collections;
using System.Linq;
using Lunra.Editor.Core;
using Lunra.Hothouse.Models;
using Lunra.NumberDemon;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lunra.Hothouse.Views.Editor
{
	[CustomEditor(typeof(DecorationView))]
	[CanEditMultipleObjects]
	public class DecorationViewEditor : PrefabEditor<DecorationView>
	{
	}
}