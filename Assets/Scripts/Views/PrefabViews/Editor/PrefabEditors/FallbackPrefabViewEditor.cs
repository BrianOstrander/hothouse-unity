using Lunra.Editor.Core;
using Lunra.Hothouse.Models;
using Lunra.Hothouse.Services.Editor;
using Lunra.StyxMvp.Models;
using UnityEditor;
using UnityEngine;

namespace Lunra.Hothouse.Views.Editor
{
	[CustomEditor(typeof(PrefabView), true, isFallback = true)]
	[CanEditMultipleObjects]
	public class FallbackPrefabViewEditor : PrefabEditor<PrefabView> { }
}