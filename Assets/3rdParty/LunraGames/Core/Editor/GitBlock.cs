using System;

namespace LunraGamesEditor
{
	[Serializable]
	public struct GitBlock
	{
		public static GitBlock Default { get { return new GitBlock(); } }

		public bool Exists;
		public bool GitFlowInitialized;
		public bool IsSubmodule;
		public string Directory;
		public string SubmodulePath;
		public string RepositoryName;
		public string MasterBranch;
		public string DevelopBranch;
		public string FeaturePrefix;
		public string ReleasePrefix;
		public string HotfixPrefix;
		public string VersionTagPrefix;
	}
}