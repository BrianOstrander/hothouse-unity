using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Rendering;

namespace Lunra.Editor.Core
{
	public static class HandlesExtensions
	{
		static Stack<CompareFunction> depthCheckStack = new Stack<CompareFunction>();

		public static void BeginDepthCheck(CompareFunction compareFunction)
		{
			depthCheckStack.Push(Handles.zTest);
			Handles.zTest = compareFunction;
		}

		public static void EndDepthCheck()
		{
			Handles.zTest = depthCheckStack.Pop();
		}
	}
}