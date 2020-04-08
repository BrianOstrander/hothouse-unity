using System.IO;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace LunraGamesEditor
{
	public static class Git
	{
		const string SubmodulePathPrefix = "gitdir: ";
		const string StatusPrefix = "On branch ";

		public static string GetCurrentSha()
		{
			var head = Path.Combine(Project.Root.FullName, ".git/logs/HEAD");

			if (!File.Exists(head)) return null;

			var lastLine = File.ReadAllLines(head).LastOrDefault();
			if (lastLine == null) return null;
			var columns = lastLine.Split(' ');
			// The latest commit is on the second column of the last row of the git HEAD file
			if (columns == null || columns.Length < 2) return null;

			return columns[1];
		}

		public static GitBlock GetContainingRepository(string directory = null)
		{
			var ceiling = Project.Root;
			var current = directory == null ? ceiling : new DirectoryInfo(directory);
			FileInfo gitFile = null;
			DirectoryInfo gitDir = null;

			while ((gitFile == null && gitDir == null) && (current != null && current != ceiling.Parent))
			{
				gitFile = current.GetFiles().FirstOrDefault(f => f.Name == ".git");
				gitDir = current.GetDirectories().FirstOrDefault(d => d.Name == ".git");
				current = current.Parent;
			}

			if (gitFile != null) return GetInfo(gitFile);
			if (gitDir != null) return GetInfo(gitDir);
			Debug.Log("nuttin");
			return GitBlock.Default;
		}

		public static GitBlock GetInfo(FileInfo submoduleRoot)
		{
			var contents = File.ReadAllText(submoduleRoot.FullName);
			if (!contents.StartsWith(SubmodulePathPrefix))
			{
				Debug.LogError("Unable to parse submodule file at " + submoduleRoot.FullName);
				return GitBlock.Default;
			}
			var realRoot = contents.Substring(SubmodulePathPrefix.Length);
			var traversal = realRoot.Trim().Split('/');
			var directory = submoduleRoot.Directory;

			foreach (var entry in traversal)
			{
				if (entry == "..") directory = directory.Parent;
				else directory = directory.GetDirectories().FirstOrDefault(d => d.Name == entry);

				if (directory == null) break;
			}

			if (directory == null)
			{
				Debug.LogError("Unable to find submodule's local repository");
				return GitBlock.Default;
			}

			var result = GitBlock.Default;
			result.IsSubmodule = true;
			result.SubmodulePath = submoduleRoot.FullName;

			return GetInfo(directory, result);
		}

		public static GitBlock GetInfo(DirectoryInfo gitRoot, GitBlock? submodule = null)
		{
			var result = submodule.HasValue ? submodule.Value : GitBlock.Default;

			result.Directory = gitRoot.FullName;
			result.Exists = true;
			result.RepositoryName = gitRoot.Name == ".git" ? gitRoot.Parent.Name : gitRoot.Name;

			var config = File.ReadAllLines(Path.Combine(gitRoot.FullName, "config"));

			var gitflowStarted = false;
			foreach (var line in config) 
			{
				var trimmed = line.Trim();

				if (trimmed == "[gitflow \"prefix\"]" || trimmed == "[gitflow \"branch\"]")
				{
					gitflowStarted = true;
					continue;
				}
				if (trimmed.StartsWith("[")) gitflowStarted = false;

				if (!gitflowStarted) continue;
				
				result.GitFlowInitialized = true;
				if (trimmed.StartsWith("[")) break;
				var keyvalue = trimmed.Split('=');
				if (0 == keyvalue.Length) continue;
				var keyTrimmed = keyvalue[0].Trim();
				var valueTrimmed = keyvalue.Length == 1 ? string.Empty : keyvalue[1].Trim();

				switch (keyTrimmed)
				{
					case "feature": result.FeaturePrefix = valueTrimmed; break;
					case "release": result.ReleasePrefix = valueTrimmed; break;
					case "hotfix": result.HotfixPrefix = valueTrimmed; break;
					case "versiontag": result.VersionTagPrefix = valueTrimmed; break;
					case "master": result.MasterBranch = valueTrimmed; break;
					case "develop": result.DevelopBranch = valueTrimmed; break;
				}
			}

			return result;
		}
	}
}