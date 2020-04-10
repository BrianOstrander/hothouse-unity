using UnityEngine;
using System.IO;

namespace LunraGamesEditor
{
	public static class Project
	{
		public static DirectoryInfo Root => new DirectoryInfo(Application.dataPath).Parent;
	}
}