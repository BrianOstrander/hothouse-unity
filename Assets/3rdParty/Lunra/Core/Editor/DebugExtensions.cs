using System.Linq;
using UnityEngine;

namespace LunraGamesEditor
{
	public static class DebugExtensions
	{
		/// <summary>
		/// Attempts to open the native C# IDE at the line that called this method.
		/// </summary>
		public static void OpenFileAtContext()
		{
			var trace = new System.Diagnostics.StackTrace(true).GetFrames();
			var first = trace.FirstOrDefault();
			foreach (var frame in trace)
			{
				var fileName = frame.GetFileName();
				if (!string.IsNullOrEmpty(fileName) && frame != first && fileName.StartsWith(Application.dataPath))
				{
					UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(fileName, frame.GetFileLineNumber());
					return;
				}
			}
			Debug.LogError("Unable to find a valid file to open");
		}
	}
}