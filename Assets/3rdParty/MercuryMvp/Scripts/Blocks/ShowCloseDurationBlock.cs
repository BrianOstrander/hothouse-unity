using System;

namespace LunraGames.SubLight
{
	[Serializable]
	public struct ShowCloseDurationBlock
	{
		public bool OverrideShow;
		public bool OverrideClose;

		public float ShowDuration;
		public float CloseDuration;
	}
}