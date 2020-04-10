using UnityEngine;
using System.IO;

namespace Lunra.Editor.Core
{
	public static class ApplicationExtensions
	{
		public static DirectoryInfo Root => new DirectoryInfo(Application.dataPath).Parent;
	}
}