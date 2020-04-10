using UnityEngine;
using System.IO;

namespace LunraGamesEditor
{
	public static class ApplicationExtensions
	{
		public static DirectoryInfo Root => new DirectoryInfo(Application.dataPath).Parent;
	}
}