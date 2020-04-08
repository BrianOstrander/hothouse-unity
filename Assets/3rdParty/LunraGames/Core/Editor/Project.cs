using UnityEngine;
using System.IO;

namespace LunraGamesEditor
{
	public static class Project
	{
		public static DirectoryInfo Root { get { return new DirectoryInfo(Application.dataPath).Parent; } }
	}
}